namespace TraitFinderApp.Model.KleiClasses.WorldGen
{
	public class SpaceDestinationMix
	{
		public int minTier { get; set; }

		public int maxTier { get; set; }

		public string type { get; set; }

		public SpaceDestinationMix()
		{
			minTier = 0;
			maxTier = 99;
		}
	}
}
