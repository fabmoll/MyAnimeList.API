[![MyAnimeList.API MyGet Build Status](https://www.myget.org/BuildSource/Badge/fabmoll?identifier=6a984c02-01fc-4620-baa9-650ebe94d0a5)](https://www.myget.org/)

# MyAnimeList.API
MyAnimeList API UWA Class Library

__How to use the API__

Just create an object from one of the services and pass the API key to the constructor :
```
AnimeService = new AnimeService("yourApiKey");
MangaService = new MangaService("yourApiKey");
AuthorizationService = new AuthorizationService("yourApiKey");
```
__Test project__

I created a project to unit test the services. You need to replace the properties in the *TestSettings* class with your credentials and your API key.

__API KEY__

To receive an API key you need to use the following form : https://atomiconline.wufoo.com/forms/mal-api-usage-notification/
