
using System.Drawing;
using TraitFinderApp.Model.KleiClasses;
using static TraitFinderApp.Client.Model.DataImport;

namespace TraitFinderApp.Client.Model
{
	public class Dlc
	{
		public static string BASEGAME_ID = "", SPACEDOUT_ID = "EXPANSION1_ID", FROSTYPLANET_ID = "DLC2_ID", BIONICBOOSTER_ID = "DLC3_ID", PREHISTORICPLANET_ID = "DLC4_ID";


		public string ID;
		//localize
		public string Name;
		public bool IsMainVersion = false;
		public string Image;
		public string BannerImage;
		public Color Color = Color.White; //default white, can be used for UI elements
		public List<spaceDestinations> extraSpaceDestinationsBasegame = [];

		public static bool TryGetDlc(string id, out Dlc dlc)
		{
			dlc = null;
			if (!id.Contains("_ID"))
				id += "_ID";
			return KeyValues.TryGetValue(id, out dlc);
		}


		public static readonly Dlc BASEGAME = new Dlc()
		{
			ID = BASEGAME_ID,
			IsMainVersion = true,
			Name = "Base Game",
			Image = "./images/logos/logo_oni.png",
		};
		public static readonly Dlc SPACEDOUT = new Dlc()
		{
			ID = SPACEDOUT_ID,
			IsMainVersion = true,
			Name = "Spaced Out!",
			Image = "./images/logos/logo_spaced_out.png",
		};
		public static readonly Dlc FROSTYPLANET = new Dlc()
		{
			ID = FROSTYPLANET_ID,
			IsMainVersion = false,
			Name = "Frosty Planet Pack",
			Image = "./images/logos/logo_frosty_planet_banner.webp",
			BannerImage = "./images/logos/dlc2_banner.png",
			extraSpaceDestinationsBasegame = [new("DLC2CeresSpaceDestination", 3, 10)],
			Color = UnityColor.Get(0.003921569f, 11f / 15f, 1f) //(80,187,255) //blue
		}; 
		public static readonly Dlc BIONICBOOSTER = new Dlc()
		{
			ID = BIONICBOOSTER_ID,
			IsMainVersion = false,
			Name = "Bionic Booster Pack",
			Image = "./images/logos/Bionic_Booster_Banner.png",
			BannerImage = "./images/logos/dlc3_banner.png",
			Color = UnityColor.Get(0.796078444f, 33f / 85f, 0.956862748f) //(203,99,244) //purple
		};
		public static readonly Dlc PREHISTORICPLANET = new Dlc()
		{
			ID = PREHISTORICPLANET_ID,
			IsMainVersion = false,
			Name = "Prehistoric Planet Pack",
			Image = "./images/logos/Prehistoric_Planet_Banner.png",
			BannerImage = "./images/logos/dlc4_banner.png",
			extraSpaceDestinationsBasegame = [new("DLC4PrehistoricSpaceDestination", 3, 10)],
			Color = UnityColor.Get(16f / 51f, 0.6745098f, 27f / 85f)//(80,172,81) //green
		};
		public static IEnumerable<Dlc> Values
		{
			get
			{
				yield return BASEGAME;
				yield return SPACEDOUT;
				yield return FROSTYPLANET;
				yield return BIONICBOOSTER;
				yield return PREHISTORICPLANET;
			}
		}
		public static Dictionary<string, Dlc> KeyValues = new()
		{
			{BASEGAME_ID,BASEGAME },
			{SPACEDOUT_ID,SPACEDOUT},
			{FROSTYPLANET_ID,FROSTYPLANET},
			{BIONICBOOSTER_ID,BIONICBOOSTER},
			{PREHISTORICPLANET_ID,PREHISTORICPLANET},
		};

		internal static void AddMixingDestinations(List<DataImport.spaceDestinations> mixingDestinations, List<Dlc> mixingDlcs)
		{
			foreach (var dlc in mixingDlcs)
			{
				mixingDestinations.AddRange(dlc.extraSpaceDestinationsBasegame);
			}
		}
	}
}
