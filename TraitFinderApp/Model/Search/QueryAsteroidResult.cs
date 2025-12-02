using TraitFinderApp.Client.Model;
using TraitFinderApp.Client.Model.Search;

namespace TraitFinderApp.Model.Search
{
	public class QueryAsteroidResult
	{
		public SearchQuery Origin;
		public Asteroid Asteroid;
		public List<WorldTrait> Traits;
		public bool IsMixing => PreMixAsteroid != null;
		public Asteroid? PreMixAsteroid = null;

		public bool RemixWasOriginally(Asteroid asteroid) => PreMixAsteroid == asteroid;

		public List<WorldTrait> GetTraitsForUI()
		{
			if (LocalStorageHelper.UsePersistentTraitOrdering)
			{
				var traitResults = new List<WorldTrait>(Traits);
				var traitsGuaranteedFirst = new List<WorldTrait>(Origin.AsteroidParams[Asteroid].Guarantee);

				traitResults.RemoveAll(item => traitsGuaranteedFirst.Contains(item));
				traitsGuaranteedFirst.AddRange(traitResults);
				return traitsGuaranteedFirst;
			}
			else
			{
				return Traits;
			}

		}


		public QueryAsteroidResult() { }
		public QueryAsteroidResult(SearchQuery _origin, Asteroid _asteroid, List<WorldTrait> _traits, Asteroid preMixAsteroid = null)
		{
			Origin = _origin;
			Asteroid = _asteroid;
			Traits = _traits;
			PreMixAsteroid = preMixAsteroid;
		}
	}
}
