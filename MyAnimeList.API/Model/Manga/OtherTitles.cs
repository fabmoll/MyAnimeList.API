using System.Collections.Generic;
using PropertyChanged;

namespace MyAnimeList.API.Model.Manga
{
	[ImplementPropertyChanged]
	public class OtherTitles
	{
		public List<string> English { get; set; }
		public List<string> Japanese { get; set; }
	}
}