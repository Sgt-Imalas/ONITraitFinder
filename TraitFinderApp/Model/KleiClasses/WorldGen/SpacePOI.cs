namespace TraitFinderApp.Model.KleiClasses.WorldGen
{
	public class POI_Data
	{
		public string Id;
		public string Name;
		public string Description;
		public string Image => "./images/Starmap/starmap_destinations_cluster/" + Path.GetFileName(Id) + ".png";
		public Dictionary<SimHashes, float>? Mineables;
		public bool HasArtifacts;

		public float RechargeMin, RechargeMax;
		public float CapacityMin, CapacityMax;
	}
}
