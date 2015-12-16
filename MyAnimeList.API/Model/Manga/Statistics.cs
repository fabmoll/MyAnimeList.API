using PropertyChanged;

namespace MyAnimeList.API.Model.Manga
{
	[ImplementPropertyChanged]
	public class Statistics
	{
		public double Days { get; set; }
	}
}