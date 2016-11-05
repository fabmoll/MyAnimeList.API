using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using MyAnimeList.API.Services;

namespace MyAnimeList.API.Tests.Services
{
    [TestClass]
    public class MangaServiceTest
    {
        private MangaService _mangaService;

        [TestInitialize]
        public void Initialize()
        {
            _mangaService = new MangaService(TestSettings.UserAgent);
        }

        [TestMethod]
        [Ignore]
        public void DeleteMangaAsync()
        {
            var result = _mangaService.DeleteMangaAsync(TestSettings.Login, TestSettings.Password, 161);

            Assert.IsTrue(result.Result);
        }


        [TestMethod]
        [Ignore]
        public void FindMangaListAsync()
        {
            var result = _mangaService.FindMangaListAsync("insy");

            Assert.IsTrue(result.Result.Manga.Count > 0);
        }

        [TestMethod]
        public void GetMangaDetailAsync()
        {
            var result = _mangaService.GetMangaDetailAsync(TestSettings.Login, TestSettings.Password, 10269);

            Assert.IsNotNull(result.Result);
        }

        [TestMethod]
        [Ignore]
        public void AddMangaAsync()
        {
            var result = _mangaService.AddMangaAsync(TestSettings.Login, TestSettings.Password, 15, "plan to read", 0, 0).Result;
        }

        [TestMethod]
        [Ignore]
        public void UpdateMangaAsync()
        {
            var result = _mangaService.UpdateMangaAsync(TestSettings.Login, TestSettings.Password, 15, "plan to read", 1, 1, 5).Result;
        }

        [TestMethod]
        [Ignore]
        public void SearchMangaAsync()
        {
            var result = _mangaService.SearchMangaAsync(TestSettings.Login, TestSettings.Password, "bleach");

            Assert.IsTrue(result.Result.Count > 0);
        }
    }
}
