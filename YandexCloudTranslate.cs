using Newtonsoft.Json;
using System.Text;

namespace YandexCloudTranslateClient;

public class YandexCloudTranslate
{
    private readonly string _apikey;
    private readonly string _folderId;
    private readonly HttpClient _httpClient;
    private const string TranslateEndpoint = "https://translate.api.cloud.yandex.net/translate/v2/translate";
    private const string LanguagesEndpoint = "https://translate.api.cloud.yandex.net/translate/v2/languages";

    public YandexCloudTranslate(string iamToken, string folderId)
    {
        _apikey = iamToken;
        _folderId = folderId;
        _httpClient = new HttpClient();

        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Api-Key", _apikey);
    }

    /// <summary>
    /// Осуществляет перевод текста с одного языка на другой.
    /// </summary>
    /// <param name="texts">Массив строк, которые необходимо перевести.</param>
    /// <param name="targetLanguage">Целевой язык перевода (например, "ru").</param>
    /// <returns>Массив переведенных строк.</returns>
    public async Task<string[]> TranslateAsync(string[] texts, string targetLanguage)
    {
        var requestBody = new
        {
            targetLanguageCode = targetLanguage,
            texts,
            folderId = _folderId,
        };

        string jsonContent = JsonConvert.SerializeObject(requestBody);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(TranslateEndpoint, content);
        if (response.IsSuccessStatusCode)
        {
            // Успешное выполнение запроса
            string jsonResponse = await response.Content.ReadAsStringAsync();
            var resultData = JsonConvert.DeserializeObject<YandexCloudTranslationResponse>(jsonResponse);
            return resultData.Translations.Select(t => t.Text).ToArray();
        }
        else
        {
            // Ошибка: проверка содержимого ошибки
            string errorResponse = await response.Content.ReadAsStringAsync();
            throw new Exception($"Не удалось перевести текст. Код ошибки: {response.StatusCode}. Ответ сервера: {errorResponse}");
        }
    }

    /// <summary>
    /// Получает список всех поддерживаемых языков с их кодами.
    /// </summary>
    /// <returns>Словарь поддерживаемых языков, где ключ - это код языка, а значение - его название.</returns>
    public async Task<Dictionary<string, string>> GetSupportedLanguagesAsync()
    {
        var requestBody = new
        {
            folderId = _folderId
        };

        string jsonContent = JsonConvert.SerializeObject(requestBody);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(LanguagesEndpoint, content);

        if (response.IsSuccessStatusCode)
        {
            string jsonResponse = await response.Content.ReadAsStringAsync();
            var data = JsonConvert.DeserializeObject<LanguageResponse>(jsonResponse);

            return data.Languages.ToDictionary(lang => lang.Code, lang => lang.Name);
        }

        throw new Exception("Не удалось получить список поддерживаемых языков");
    }

    private record YandexCloudTranslationResponse([property: JsonProperty("translations")] YandexCloudTranslation[] Translations);

    private record YandexCloudTranslation([property: JsonProperty("text")] string Text);

    private record LanguageResponse([property: JsonProperty("languages")] Language[] Languages);

    private record Language([property: JsonProperty("code")] string Code, [property: JsonProperty("name")] string Name);
}