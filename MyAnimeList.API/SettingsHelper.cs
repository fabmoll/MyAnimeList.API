using Windows.Storage;

namespace MyAnimeList.API
{
    public static class SettingsHelper
    {

        /// <summary>
        /// Set setting value
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static void Set<T>(string key, T value)
        {

            ApplicationData.Current.LocalSettings.Values[key] = value;
        }


        /// <summary>
        /// Check if the key exists
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static bool Contains(string key)
        {
            bool isContains = false;

            isContains = ApplicationData.Current.LocalSettings.Values.Keys.Contains(key);
            return isContains;
        }


        /// <summary>
        /// Gets value
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public static T Get<T>(string key)
        {
            T value = default(T);

            value = (T)ApplicationData.Current.LocalSettings.Values[key];
            return value;
        }


        /// <summary>
        /// Gets value, returns default value if value does not exist
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static T Get<T>(string key, T defaultValue)
        {
            bool isContains = SettingsHelper.Contains(key);
            if (!isContains)
            {
                return defaultValue;
            }
            return Get<T>(key);
        }

        /// <summary>
        /// Removes a value from the settings
        /// </summary>
        /// <param name="key"></param>
        public static void Remove(string key)
        {
            ApplicationData.Current.LocalSettings.Values.Remove(key);

        }
    }
}