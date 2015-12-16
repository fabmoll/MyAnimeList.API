using PropertyChanged;

namespace MyAnimeList.API.Model.Anime
{
	[ImplementPropertyChanged]
	public class Statistics
	{
		public double Days { get; set; }
	}
}