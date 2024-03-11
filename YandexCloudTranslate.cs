using Newtonsoft.Json;
using System.Text;

namespace YandexCloudTranslateClient;

public class YandexCloudTranslate
{
    /// <summary>
    /// Docs:
    /// https://cloud.yandex.ru/ru/docs/translate/operations/translate
    /// </summary>
    private readonly string _apikey;

    /// <summary>
    /// https://cloud.yandex.ru/ru/docs/translate/operations/translate#before-begin
    /// </summary>
    private readonly string _folderId;
    private readonly HttpClient _httpClient;
    private const string TranslateEndpoint = "https://translate.api.cloud.yandex.net/translate/v2/translate";
    private const string LanguagesEndpoint = "https://translate.api.cloud.yandex.net/translate/v2/languages";
    private const string DetectLanguageEndpoint = "https://translate.api.cloud.yandex.net/translate/v2/detect";

    /// <param name="iamToken"></param>
    /// <param name="folderId">https://cloud.yandex.ru/docs/iam/api-ref/UserAccount#representation</param>
    public YandexCloudTranslate(string iamToken, string folderId)
    {
        _apikey = iamToken;
        _folderId = folderId;
        _httpClient = new HttpClient();

        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Api-Key", _apikey);
    }

    /// <summary>
    /// Осуществляет перевод текста с одного языка на другой. Перевод текста не обязательно должен быть по словам, в строку можно передавать сочетание слов.
    /// </summary>
    /// <param name="texts">Массив строк, которые необходимо перевести.</param>
    /// <param name="targetLanguage">Целевой язык перевода (например, "ru").</param>
    /// <param name="speller">Параметр, который включает проверку орфографии. https://cloud.yandex.ru/ru/docs/translate/operations/better-quality#with-speller</param>
    /// <returns>Массив переведенных строк.</returns>
    public async Task<string[]> TranslateAsync(string[] texts, string targetLanguage, bool speller = false)
    {
        var requestBody = new
        {
            targetLanguageCode = targetLanguage,
            texts,
            folderId = _folderId,
            speller,
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
    /// Есть слова, которые пишутся одинаково в разных языках, но переводятся по-разному. 
    /// Например, слово angel в английском языке означает духовное существо, а в немецком — удочку. 
    /// Если переданный текст состоит из таких слов, то Translate может ошибиться при определении языка текста. 
    /// Чтобы избежать ошибки, укажите в поле sourceLanguage язык, с которого необходимо перевести текст.
    /// </summary>
    /// <param name="texts"></param>
    /// <param name="sourceLanguage"></param>
    /// <param name="targetLanguage"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public async Task<string[]> TranslateAsync(string[] texts, string sourceLanguage, string targetLanguage)
    {
        var requestBody = new
        {
            folderId = _folderId,
            texts,
            targetLanguageCode = targetLanguage,
            sourceLanguageCode = sourceLanguage,
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

    /// <summary>
    /// Определяет язык текста.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="languageCodeHintsInput"></param>
    /// <returns>(string) languageCode - Язык текста(например, ru).</returns>
    /// <exception cref="Exception"></exception>
    public async Task<string> DetectLanguage(string input, string[]? languageCodeHintsInput = null)
    {
        if(input.Length > 1000) throw new Exception("Max 1000 symbols to detect");

        var requestBody = new
        {
            text = input,
            languageCodeHints = languageCodeHintsInput ?? new string[]
            {
                "ru",
            },
            folderId = _folderId
        };

        string jsonContent = JsonConvert.SerializeObject(requestBody);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(DetectLanguageEndpoint, content);

        if (response.IsSuccessStatusCode)
        {
            string jsonResponse = await response.Content.ReadAsStringAsync();
            var data = JsonConvert.DeserializeObject<LanguageDetectResponse>(jsonResponse);

            return data.LanguageCode;
        }

        throw new Exception("Не удалось определить язык текста.");
    }

    private record LanguageDetectResponse([property: JsonProperty("languageCode")] string LanguageCode);

    private record YandexCloudTranslationResponse([property: JsonProperty("translations")] YandexCloudTranslation[] Translations);

    private record YandexCloudTranslation([property: JsonProperty("text")] string Text);

    private record LanguageResponse([property: JsonProperty("languages")] Language[] Languages);

    private record Language([property: JsonProperty("code")] string Code, [property: JsonProperty("name")] string Name);
}