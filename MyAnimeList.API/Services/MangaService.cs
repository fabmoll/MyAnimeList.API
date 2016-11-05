using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Windows.Security.Credentials;
using MyAnimeList.API.Model;
using MyAnimeList.API.Model.Manga;
using MyAnimeList.API.ServicesContracts;
using PCLWebUtility;

namespace MyAnimeList.API.Services
{
    public class MangaService : BaseService, IMangaService
    {
        public MangaService(string userAgent) : base(userAgent)
        {
        }

        public async Task<MangaRoot> FindMangaListAsync(string login)
        {
            var parameters = new Dictionary<string, string> { { "u", login }, { "status", "all" }, { "type", "manga" } };

            var data = parameters.ToQueryString();

            var result = await GetAsync(string.Format("malappinfo.php?{0}", data));

            try
            {
                var xDocument = XDocument.Parse(result).Root;

                var mangaRoot = new MangaRoot() { Manga = new List<Manga>(), Statistics = new Statistics() };

                if (xDocument.Element("myinfo") != null && xDocument.Element("myinfo").Element("user_days_spent_watching") != null)
                    mangaRoot.Statistics.Days = xDocument.Element("myinfo").ElementValue("user_days_spent_watching", 0.0d);

                foreach (var mangaElement in xDocument.Elements("manga"))
                {
                    var manga = new Manga
                    {
                        Id = mangaElement.ElementValue("series_mangadb_id", 0),
                        Title = mangaElement.Element("series_title").Value,
                        Type = GetType(mangaElement.ElementValue("series_type", 0)),
                        Status = GetStatus(mangaElement.ElementValue("series_status", 0)),
                        Chapters = mangaElement.ElementValue("series_chapters", 0),
                        Volumes = mangaElement.ElementValue("series_volumes", 0),
                        ImageUrl = mangaElement.Element("series_image").Value,
                        ListedMangaId = mangaElement.ElementValue("my_id", 0),
                        VolumesRead = mangaElement.ElementValue("my_read_volumes", 0),
                        ChaptersRead = mangaElement.ElementValue("my_read_chapters", 0),
                        Score = mangaElement.ElementValue("my_score", 0),
                        ReadStatus = GetReadStatus(mangaElement.ElementValue("my_status", 0))
                    };
                    mangaRoot.Manga.Add(manga);
                }

                return mangaRoot;
            }
            catch (XmlException exception)
            {
                throw new ServiceException("Unable to perform the action", exception.InnerException);
            }
        }

        public async Task<MangaDetail> GetMangaDetailAsync(string login, string password, int mangaId)
        {
            await InitializeCookie(login, password);

            var result = await GetAsync(string.Format("/manga/{0}", mangaId));

            var mangaDetail = new MangaDetail();

            var document = new HtmlAgilityPack.HtmlDocument();

            document.LoadHtml(result);

            var mangaIdInput =
                document.DocumentNode.Descendants("input")
                    .FirstOrDefault(input => input.GetAttributeValue("name", null) == "mid");

            //Get Manga Id
            //Example: <input type="hidden" value="104" name="mid" />
            if (mangaIdInput != null)
            {
                mangaDetail.Id = Convert.ToInt32(mangaIdInput.Attributes["value"].Value);
            }
            else
            {
                var detailLink =
                    document.DocumentNode.Descendants("a")
                        .FirstOrDefault(a => a.InnerText == "Details");

                if (detailLink != null)
                {
                    var regex = Regex.Match(detailLink.Attributes["href"].Value, @"\d+");
                    mangaDetail.Id = Convert.ToInt32(regex.ToString());
                }
            }


            //Title and rank.
            //Example:
            //# <h1><div style="float: right; font-size: 13px;">Ranked #96</div>Lucky ☆ Star</h1>
            var rankNode = document.DocumentNode.Descendants("span").FirstOrDefault(c => c.InnerText.Contains("Rank"));

            if (rankNode != null)
            {
                if (rankNode.NextSibling.InnerText.ToUpper().Contains("N/A"))
                    mangaDetail.Rank = 0;
                else
                {
                    var regex = Regex.Match(rankNode.NextSibling.InnerText, @"\d+");
                    mangaDetail.Rank = Convert.ToInt32(regex.ToString());
                }
            }

            var titleNode =
                document.DocumentNode.Descendants("span")
                    .FirstOrDefault(span => span.GetAttributeValue("itemprop", null) == "name");


            if (titleNode != null)
                mangaDetail.Title = WebUtility.HtmlDecode(titleNode.InnerText.Trim());

            //Image URL

            var imageNode =
               document.DocumentNode.Descendants("div")
                   .FirstOrDefault(div => div.GetAttributeValue("id", null) == "content")
                   .Descendants("tr")
                   .FirstOrDefault()
                   .Descendants("td")
                   .FirstOrDefault()
                   .Descendants("div")
                   .FirstOrDefault()
                   .Descendants("img")
                   .FirstOrDefault();

            if (imageNode != null)
                mangaDetail.ImageUrl = imageNode.Attributes["src"].Value;

            var leftColumnNodeset =
                document.DocumentNode.Descendants("div")
                    .FirstOrDefault(div => div.GetAttributeValue("id", null) == "content")
                    .Descendants("table").FirstOrDefault()
                    .Descendants("tr").FirstOrDefault()
                    .Descendants("td").FirstOrDefault(td => td.GetAttributeValue("class", null) == "borderClass");

            if (leftColumnNodeset != null)
            {
                var englishAlternative =
                    leftColumnNodeset.Descendants("span")
                        .FirstOrDefault(span => span.InnerText == "English:");

                mangaDetail.OtherTitles = new OtherTitles();

                if (englishAlternative != null)
                {
                    mangaDetail.OtherTitles.English = englishAlternative.NextSibling.InnerText.Split(',').Select(p => p.Trim()).ToList();
                }

                var japaneseAlternative =
                    leftColumnNodeset.Descendants("span")
                        .FirstOrDefault(span => span.InnerText == "Japanese:");

                if (japaneseAlternative != null)
                {
                    mangaDetail.OtherTitles.Japanese = japaneseAlternative.NextSibling.InnerText.Split(',').Select(p => p.Trim()).ToList();
                }

                var type =
                    leftColumnNodeset.Descendants("span")
                        .FirstOrDefault(span => span.InnerText.Contains("Type:"));

                if (type != null)
                    mangaDetail.Type = type.NextSibling.NextSibling.InnerText.Trim();

                var volume = leftColumnNodeset.Descendants("span")
                       .FirstOrDefault(span => span.InnerText == "Volumes:");
                if (volume != null)
                {
                    int volumes;
                    if (Int32.TryParse(volume.NextSibling.InnerText.Replace(",", ""), out volumes))
                        mangaDetail.Volumes = volumes;
                    else
                    {
                        mangaDetail.Volumes = null;
                    }
                }

                var chapter = leftColumnNodeset.Descendants("span")
                       .FirstOrDefault(span => span.InnerText == "Chapters:");
                if (chapter != null)
                {
                    int chapters;
                    if (Int32.TryParse(chapter.NextSibling.InnerText.Replace(",", ""), out chapters))
                        mangaDetail.Chapters = chapters;
                    else
                    {
                        mangaDetail.Chapters = null;
                    }
                }

                var status =
                    leftColumnNodeset.Descendants("span")
                        .FirstOrDefault(span => span.InnerText == "Status:");

                if (status != null)
                    mangaDetail.Status = status.NextSibling.InnerText;

                var genre =
                   leftColumnNodeset.Descendants("span")
                       .FirstOrDefault(span => span.InnerText == "Genres:");

                if (genre != null)
                {
                    mangaDetail.Genres = genre.ParentNode.ChildNodes.Where(c => c.Name == "a").Select(x => x.InnerText.Trim()).ToList();
                }

                var score =
                   leftColumnNodeset.Descendants("span")
                       .FirstOrDefault(span => span.InnerText == "Score:");

                if (score != null)
                {
                    double memberScore;
                    if (double.TryParse(score.NextSibling.NextSibling.InnerText, NumberStyles.Any, CultureInfo.InvariantCulture, out memberScore))
                        mangaDetail.MembersScore = memberScore;
                    else
                    {
                        mangaDetail.MembersScore = 0;
                    }
                }

                var popularity =
                    leftColumnNodeset.Descendants("span")
                        .FirstOrDefault(span => span.InnerText == "Popularity:");

                if (popularity != null)
                {
                    int popularityRank;

                    if (Int32.TryParse(popularity.NextSibling.InnerText.Replace("#", "").Replace(",", ""), out popularityRank))
                        mangaDetail.PopularityRank = popularityRank;
                    else
                    {
                        mangaDetail.PopularityRank = null;
                    }
                }
                var member =
                    leftColumnNodeset.Descendants("span")
                        .FirstOrDefault(span => span.InnerText == "Members:");

                if (member != null)
                {
                    int memberCount;
                    if (Int32.TryParse(member.NextSibling.InnerText.Replace(",", ""), out memberCount))
                        mangaDetail.MembersCount = memberCount;
                    else
                    {
                        mangaDetail.MembersCount = null;
                    }
                }

                var favorite =
                    leftColumnNodeset.Descendants("span")
                        .FirstOrDefault(span => span.InnerText == "Favorites:");

                if (favorite != null)
                {
                    int favoritedCount;
                    if (Int32.TryParse(favorite.NextSibling.InnerText.Replace(",", ""), out favoritedCount))
                        mangaDetail.FavoritedCount = favoritedCount;
                    else
                    {
                        mangaDetail.FavoritedCount = null;
                    }
                }
            }

            var rightColumnNodeset =
               document.DocumentNode.Descendants("h2")
                   .FirstOrDefault(h2 => h2.LastChild.InnerText == "Synopsis").ParentNode.ParentNode.ParentNode;

            if (rightColumnNodeset != null)
            {
                var synopsis =
                rightColumnNodeset.Descendants("h2")
                    .FirstOrDefault(h2 => h2.LastChild.InnerText == "Synopsis");

                if (synopsis != null)
                {
                    mangaDetail.Synopsis = Regex.Replace(WebUtility.HtmlDecode(synopsis.NextSibling.InnerText), "<br>", "");
                }

                var relatedManga =
                    rightColumnNodeset.Descendants("h2")
                        .FirstOrDefault(h2 => h2.LastChild.InnerText == "Related Manga");

                if (relatedManga != null)
                {
                    //Alternative
                    var alternative =
                         Regex.Match(
                              relatedManga.ParentNode.InnerHtml.Substring(relatedManga.ParentNode.InnerHtml.IndexOf("<h2>")),
                              "Alternative versions:+(.+?<br)");

                    if (!string.IsNullOrEmpty(alternative.ToString()))
                    {

                        mangaDetail.AlternativeVersions = new List<MangaSummary>();

                        SetMangaSummaryList(mangaDetail.AlternativeVersions, alternative.ToString());
                    }

                    //Adaptation
                    var adaptation =
                        Regex.Match(
                            relatedManga.ParentNode.InnerHtml.Substring(relatedManga.ParentNode.InnerHtml.IndexOf("<h2>")),
                            "Adaptation:+(.+?<br)");

                    if (!string.IsNullOrEmpty(adaptation.ToString()))
                    {
                        mangaDetail.AnimeAdaptations = new List<AnimeSummary>();

                        SetAnimeSummaryList(mangaDetail.AnimeAdaptations, adaptation.ToString());
                    }


                    var prequel =
                    Regex.Match(
                        relatedManga.ParentNode.InnerHtml.Substring(relatedManga.ParentNode.InnerHtml.IndexOf("<h2>")),
                        "Prequel:+(.+?<br)");

                    mangaDetail.RelatedManga = new List<MangaSummary>();

                    if (!string.IsNullOrEmpty(prequel.ToString()))
                    {
                        SetMangaSummaryList(mangaDetail.RelatedManga, prequel.ToString());
                    }

                    var sequel =
                        Regex.Match(relatedManga.ParentNode.InnerHtml.Substring(relatedManga.ParentNode.InnerHtml.IndexOf("<h2>")),
                            "Sequel:+(.+?<br)");

                    if (!string.IsNullOrEmpty(sequel.ToString()))
                    {
                        SetMangaSummaryList(mangaDetail.RelatedManga, sequel.ToString());
                    }

                    var parentStory =
                    Regex.Match(relatedManga.ParentNode.InnerHtml.Substring(relatedManga.ParentNode.InnerHtml.IndexOf("<h2>")),
                        "Parent story:+(.+?<br)");

                    if (!string.IsNullOrEmpty(parentStory.ToString()))
                    {
                        SetMangaSummaryList(mangaDetail.RelatedManga, parentStory.ToString());
                    }

                    var sideStory =
                        Regex.Match(relatedManga.ParentNode.InnerHtml.Substring(relatedManga.ParentNode.InnerHtml.IndexOf("<h2>")),
                            "Side story:+(.+?<br)");


                    if (!string.IsNullOrEmpty(sideStory.ToString()))
                    {
                        SetMangaSummaryList(mangaDetail.RelatedManga, sideStory.ToString());
                    }

                    var character =
                    Regex.Match(relatedManga.ParentNode.InnerHtml.Substring(relatedManga.ParentNode.InnerHtml.IndexOf("<h2>")),
                        "Character:+(.+?<br)");

                    if (!string.IsNullOrEmpty(character.ToString()))
                    {
                        SetMangaSummaryList(mangaDetail.RelatedManga, character.ToString());
                    }

                    var spinOff =
                        Regex.Match(relatedManga.ParentNode.InnerHtml.Substring(relatedManga.ParentNode.InnerHtml.IndexOf("<h2>")),
                            "Spin-off:+(.+?<br)");


                    if (!string.IsNullOrEmpty(spinOff.ToString()))
                    {
                        SetMangaSummaryList(mangaDetail.RelatedManga, spinOff.ToString());
                    }

                    var summary =
                    Regex.Match(relatedManga.ParentNode.InnerHtml.Substring(relatedManga.ParentNode.InnerHtml.IndexOf("<h2>")),
                        "Summary:+(.+?<br)");

                    if (!string.IsNullOrEmpty(summary.ToString()))
                    {
                        SetMangaSummaryList(mangaDetail.RelatedManga, summary.ToString());
                    }
                }
            }


            var readStatusNode =
           document.DocumentNode.Descendants("select")
               .FirstOrDefault(select => select.GetAttributeValue("id", null) == "myinfo_status" && select.InnerHtml.ToUpper().Contains("SELECTED"));

            if (readStatusNode != null)
            {
                var selectedOption =
                     readStatusNode.ChildNodes.Where(c => c.Name.ToLowerInvariant() == "option");

                var selected = from c in selectedOption
                               from x in c.Attributes
                               where x.Name.ToLowerInvariant() == "selected"
                               select c;

                if (selected.FirstOrDefault() != null)
                    mangaDetail.ReadStatus = selected.FirstOrDefault().NextSibling.InnerText;
            }

            var chapthersReadNode = document.DocumentNode.Descendants("input")
                .FirstOrDefault(select => select.GetAttributeValue("id", null) == "myinfo_chapters");

            if (chapthersReadNode != null)
            {
                var value =
                    chapthersReadNode.Attributes.FirstOrDefault(c => c.Name.ToLowerInvariant() == "value");

                if (value != null)
                {
                    int chaptersRead;

                    if (Int32.TryParse(value.Value, out chaptersRead))
                        mangaDetail.ChaptersRead = chaptersRead;
                    else
                    {
                        mangaDetail.ChaptersRead = 0;
                    }
                }
            }

            var volumesReadNode = document.DocumentNode.Descendants("input")
                .FirstOrDefault(select => select.GetAttributeValue("id", null) == "myinfo_volumes");

            if (volumesReadNode != null)
            {
                var value =
                    volumesReadNode.Attributes.FirstOrDefault(c => c.Name.ToLowerInvariant() == "value");

                if (value != null)
                {
                    int volumesRead;

                    if (Int32.TryParse(value.Value, out volumesRead))
                        mangaDetail.VolumesRead = volumesRead;
                    else
                    {
                        mangaDetail.VolumesRead = 0;
                    }
                }
            }

            var myScoreNode =
                document.DocumentNode.Descendants("select")
                    .FirstOrDefault(select => select.GetAttributeValue("id", null) == "myinfo_score");

            if (myScoreNode != null)
            {
                var selectedOption =
                     myScoreNode.ChildNodes.Where(c => c.Name.ToLowerInvariant() == "option");

                var selected = from c in selectedOption
                               from x in c.Attributes
                               where x.Name.ToLowerInvariant() == "selected"
                               select c;

                if (selected.FirstOrDefault() != null)
                {
                    var scoreNode = from c in selected.FirstOrDefault().Attributes
                                    where c.Name.ToLowerInvariant() == "value"
                                    select c;

                    var score = scoreNode.FirstOrDefault();

                    if (score != null)
                        mangaDetail.Score = score.Value == null ? 0 : Convert.ToInt32(score.Value);
                }
            }

            var editDetailNode =
              document.DocumentNode.Descendants("a")
                  .FirstOrDefault(a => a.InnerText == "Edit Details");

            if (editDetailNode != null)
            {
                var hrefValue = editDetailNode.Attributes["href"].Value;

                var regex = Regex.Match(hrefValue, @"\d+");

                mangaDetail.ListedMangaId = Convert.ToInt32(regex.ToString());
            }


            return mangaDetail;
        }

        public async Task<bool> AddMangaAsync(string login, string password, int mangaId, string status, int chaptersRead, int score)
        {
            var data = CreateMangaValue(GetReadStatus(status), chaptersRead, 0, score);

            var dictionnary = new Dictionary<string, string>();

            dictionnary.Add("data", data);

            var response = await PostAsync(string.Format("/api/mangalist/add/{0}.xml", mangaId), dictionnary,
                new PasswordCredential("pass", login, password));

            if (response.ToLowerInvariant().Contains("201 created"))
                return true;

            return false;
        }

        public async Task<bool> UpdateMangaAsync(string login, string password, int mangaId, string status, int chaptersRead, int volume, int score)
        {
            var data = CreateMangaValue(GetReadStatus(status), chaptersRead, 0, score);

            var dictionnary = new Dictionary<string, string>();

            dictionnary.Add("data", data);

            var response = await PostAsync(string.Format("/api/mangalist/update/{0}.xml", mangaId), dictionnary,
                new PasswordCredential("pass", login, password));

            if (response.ToLowerInvariant().Contains("updated"))
                return true;

            return false;
        }

        public Task<List<MangaDetailSearchResult>> SearchMangaAsync(string searchCriteria)
        {
            throw new NotImplementedException();
        }

        public async Task<List<MangaDetailSearchResult>> SearchMangaAsync(string login, string password, string searchCriteria)
        {
            var data = new Dictionary<string, string>();

            data.Add("q", searchCriteria);

            var result = await PostAsync("/api/manga/search.xml", data, new PasswordCredential("pass", login, password));


            result = result.Replace("~", "");

            try
            {
                //Weird method to remove special html char and then re-add some of them with the for loop to parse the xml... (for '&' value)
                result = WebUtility.HtmlDecode(WebUtility.HtmlDecode(result));

                string resultEncoded = "";
                for (int i = 0; i < result.Length; i++)
                {
                    if (result[i] == '<' || result[i] == '>' || result[i] == '"')
                    {
                        resultEncoded += result[i];
                    }
                    else
                    {
                        resultEncoded += WebUtility.HtmlEncode(result[i].ToString());
                    }
                }

                string regExp = @"</(\w+)>";
                MatchCollection mc = Regex.Matches(resultEncoded, regExp);
                foreach (Match m in mc)
                {
                    string val = m.Groups[1].Value;
                    string regExp2 = "<" + val + "( |>)";
                    Match m2 = Regex.Match(resultEncoded, regExp2);
                    if (m2.Success)
                    {
                        char[] chars = resultEncoded.ToCharArray();
                        chars[m2.Index] = '~';
                        resultEncoded = new string(chars);
                        resultEncoded = Regex.Replace(resultEncoded, @"</" + val + ">", "~/" + val + ">");
                    }

                    resultEncoded = Regex.Replace(resultEncoded, @"<\?", @"~?"); // declarations
                    resultEncoded = Regex.Replace(resultEncoded, @"<!", @"~!");   // comments
                }

                string regExp3 = @"<\w+\s?/>";
                Match m3 = Regex.Match(resultEncoded, regExp3);
                if (m3.Success)
                {
                    char[] chars = resultEncoded.ToCharArray();
                    chars[m3.Index] = '~';
                    resultEncoded = new string(chars);
                }
                resultEncoded = Regex.Replace(resultEncoded, "<", "&lt;");
                resultEncoded = Regex.Replace(resultEncoded, "~", "<");
                resultEncoded = Regex.Replace(resultEncoded, " & ", " and ");


                var xDocument = XDocument.Parse(resultEncoded).Root;

                var mangaDetailSearchResults = new List<MangaDetailSearchResult>();


                foreach (var mangaElement in xDocument.Elements("entry"))
                {
                    var searchResult = new MangaDetailSearchResult
                    {
                        Title = mangaElement.Element("title").Value,
                        MembersScore = 0,
                        Type = mangaElement.Element("type").Value,
                        ImageUrl = mangaElement.Element("image").Value,
                        Synopsis = WebUtility.HtmlDecode(mangaElement.Element("synopsis").Value),
                        Volumes = 0,
                        Id = mangaElement.ElementValue("id", 0),
                        Chapters = 0
                    };

                    mangaDetailSearchResults.Add(searchResult);
                }

                return mangaDetailSearchResults;
            }
            catch (XmlException exception)
            {
                throw new ServiceException("Cannot parse the search result", exception.InnerException);
            }
        }

        public async Task<bool> DeleteMangaAsync(string login, string password, int mangaId)
        {
            var result = await DeleteAsync(string.Format("api/mangalist/delete/{0}.xml", mangaId), new PasswordCredential("pass", login, password));

            if (!string.IsNullOrEmpty(result) && result.ToLowerInvariant() == "deleted")
                return true;

            return false;
        }

        private string GetStatus(int status)
        {
            switch (status)
            {
                case 1:
                    return "publishing";

                case 2:
                    return "finished";

                case 3:
                    return "not yet published";

                default:
                    return "finished";
            }
        }

        private string GetReadStatus(int status)
        {
            switch (status)
            {
                case 1:
                    return "reading";

                case 2:
                    return "completed";

                case 3:
                    return "on-hold";

                case 4:
                    return "dropped";
                case 6:
                    return "plan to read";

                default:
                    return "reading";
            }
        }

        private int GetReadStatus(string status)
        {
            switch (status.ToLowerInvariant())
            {
                case "reading":
                    return 1;

                case "completed":
                    return 2;

                case "on-hold":
                    return 3;

                case "dropped":
                    return 4;

                case "plan to read":
                    return 6;

                default:
                    return 6;
            }
        }

        private string GetType(int type)
        {
            switch (type)
            {
                case 1:
                    return "Manga";

                case 2:
                    return "Novel";

                case 3:
                    return "One Shot";

                case 4:
                    return "Doujin";

                case 5:
                    return "Manwha";

                case 6:
                    return "Manhua";

                case 7:
                    // Original English Language
                    return "OEL";

                default:
                    return "Manga";
            }
        }

        private string CreateMangaValue(int status, int chaptersRead, int volumesRead, int score)
        {
            var xml = new StringBuilder();

            //if values are not set they're reseted on MAL
            xml.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            xml.AppendLine("<entry>");
            xml.AppendLine("<chapter>" + chaptersRead + "</chapter>");
            xml.AppendLine("<volume>" + volumesRead + "</volume>");
            xml.AppendLine("<status>" + status + "</status>");
            xml.AppendLine("<score>" + score + "</score>");
            //xml.AppendLine("<downloaded_chapters></downloaded_chapters>");
            //xml.AppendLine("<times_reread></times_reread>");
            //xml.AppendLine("<reread_value></reread_value>");
            //xml.AppendLine("<date_start></date_start>");
            //xml.AppendLine("<date_finish></date_finish>");
            //xml.AppendLine("<priority></priority>");
            //xml.AppendLine("<enable_discussion></enable_discussion>");
            //xml.AppendLine("<enable_rereading></enable_rereading>");
            //xml.AppendLine("<comments></comments>");
            //xml.AppendLine("<scan_group></scan_group>");
            //xml.AppendLine("<tags></tags>");
            //xml.AppendLine("<retail_volumes></retail_volumes>");
            xml.AppendLine("</entry>");


            return xml.ToString();
        }


        private void SetMangaSummaryList(List<MangaSummary> mangaSummaries, string htmlContent)
        {
            var relatedDocument = new HtmlAgilityPack.HtmlDocument();

            relatedDocument.LoadHtml(htmlContent);

            foreach (var alternativeNode in relatedDocument.DocumentNode.ChildNodes.Where(c => c.Name == "a").Select(x => x))
            {
                var mangaSummary = new MangaSummary
                {
                    Url = alternativeNode.Attributes["href"].Value,
                    Title = alternativeNode.InnerText
                };

                var stringToParse = alternativeNode.Attributes["href"].Value.Replace(
                     "/manga/", "");


                var mangaIdString = stringToParse.Substring(0, stringToParse.IndexOf("/", StringComparison.Ordinal));

                int mangaAlternativeId;

                if (Int32.TryParse(mangaIdString, out mangaAlternativeId))
                {
                    mangaSummary.MangaId = mangaAlternativeId;
                }
                else
                {
                    mangaSummary.MangaId = 0;
                }

                mangaSummaries.Add(mangaSummary);
            }
        }

        private void SetAnimeSummaryList(List<AnimeSummary> animeSummaries, string htmlContent)
        {
            var relatedDocument = new HtmlAgilityPack.HtmlDocument();

            relatedDocument.LoadHtml(htmlContent);

            foreach (var alternativeNode in relatedDocument.DocumentNode.Descendants("a"))
            /*SelectNodes("//a[@href]").Select(x => x))*/
            {
                var animeSummary = new AnimeSummary
                {
                    Url = alternativeNode.Attributes["href"].Value,
                    Title = alternativeNode.InnerText
                };

                var stringToParse = alternativeNode.Attributes["href"].Value.Replace(
                     "/anime/", "");


                var mangaIdString = stringToParse.Substring(0, stringToParse.IndexOf("/", StringComparison.Ordinal));

                int mangaAlternativeId;

                if (Int32.TryParse(mangaIdString, out mangaAlternativeId))
                {
                    animeSummary.AnimeId = mangaAlternativeId;
                }
                else
                {
                    animeSummary.AnimeId = 0;
                }

                animeSummaries.Add(animeSummary);
            }
        }

    }
}