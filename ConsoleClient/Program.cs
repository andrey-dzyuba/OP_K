using System.Net.Http.Headers;
using System.Text.Json;

namespace ConsoleClient
{
	class Program
	{
		private static readonly HttpClient _client = new HttpClient();
		private static string _token = string.Empty;

		static async Task Main(string[] args)
		{
			// Укажите ваш порт
			_client.BaseAddress = new Uri("http://localhost:5296");

			while (true)
			{
				Console.WriteLine();
				Console.WriteLine("=== Команды: ===");
				Console.WriteLine("1) register  - регистрация");
				Console.WriteLine("2) login     - вход (получить токен)");
				Console.WriteLine("3) texts     - показать все тексты");
				Console.WriteLine("4) encrypt   - зашифровать текст");
				Console.WriteLine("5) decrypt   - дешифровать текст");
				Console.WriteLine("6) add       - добавить новый текст");
				Console.WriteLine("0) exit      - выход");

				Console.Write(">> ");

				var command = Console.ReadLine()?.ToLower().Trim();
				if (command == "exit" || command == "0") break;

				try
				{
					switch (command)
					{
						case "register":
							await RegisterUserAsync();
							break;
						case "login":
							await LoginAsync();
							break;
						case "texts":
							await GetAllTextsAsync();
							break;
						case "encrypt":
							await EncryptTextAsync();
							break;
						case "decrypt":
							await DecryptTextAsync();
							break;
						case "add":
							await AddTextAsync();
							break;
						case "exit":
						case "0":
							return;
						default:
							Console.WriteLine("Неизвестная команда.");
							break;
					}
				}
				catch (Exception ex)
				{
					Console.WriteLine("Ошибка: " + ex.Message);
				}
			}
		}

		// (1) Регистрация (через query string)
		private static async Task RegisterUserAsync()
		{
			Console.Write("Имя пользователя: ");
			string username = Console.ReadLine() ?? "";

			Console.Write("Пароль: ");
			string password = Console.ReadLine() ?? "";

			// Формируем URL: POST /register?username=...&password=...
			string url = $"/register?username={Uri.EscapeDataString(username)}&password={Uri.EscapeDataString(password)}";

			// Пустое тело (content: null), т.к. все данные в query string
			var response = await _client.PostAsync(url, null);

			if (response.IsSuccessStatusCode)
			{
				string json = await response.Content.ReadAsStringAsync();
				var result = JsonSerializer.Deserialize<Dictionary<string, string>>(json);

				if (result != null && result.ContainsKey("token"))
				{
					_token = result["token"];
					Console.WriteLine("Регистрация прошла успешно!");
					Console.WriteLine("Ваш токен: " + _token);
				}
				else
				{
					Console.WriteLine("Не удалось получить токен после регистрации.");
				}
			}
			else
			{
				Console.WriteLine("Ошибка при регистрации. Код статуса: " + response.StatusCode);
				Console.WriteLine("Текст ответа: " + await response.Content.ReadAsStringAsync());
			}
		}

		// (2) Логин (через query string)
		private static async Task LoginAsync()
		{
			Console.Write("Имя пользователя: ");
			string username = Console.ReadLine() ?? "";

			Console.Write("Пароль: ");
			string password = Console.ReadLine() ?? "";

			// POST /login?username=...&password=...
			string url = $"/login?username={Uri.EscapeDataString(username)}&password={Uri.EscapeDataString(password)}";

			var response = await _client.PostAsync(url, null);

			if (response.IsSuccessStatusCode)
			{
				string json = await response.Content.ReadAsStringAsync();
				var result = JsonSerializer.Deserialize<Dictionary<string, string>>(json);

				if (result != null && result.ContainsKey("token"))
				{
					_token = result["token"];
					Console.WriteLine("Логин успешен!");
					Console.WriteLine("Ваш токен: " + _token);
				}
				else
				{
					Console.WriteLine("Не удалось получить токен после логина.");
				}
			}
			else
			{
				Console.WriteLine("Ошибка при логине. Код статуса: " + response.StatusCode);
				Console.WriteLine("Текст ответа: " + await response.Content.ReadAsStringAsync());
			}
		}

		// (3) Получить все тексты (GET /text)
		private static async Task GetAllTextsAsync()
		{
			if (string.IsNullOrWhiteSpace(_token))
			{
				Console.WriteLine("Сначала необходимо залогиниться или зарегистрироваться (токен пуст).");
				return;
			}

			// Устанавливаем заголовок "Authorization: Bearer <token>"
			_client.DefaultRequestHeaders.Authorization =
				new AuthenticationHeaderValue("Bearer", _token);

			var response = await _client.GetAsync("/text");
			if (response.IsSuccessStatusCode)
			{
				string json = await response.Content.ReadAsStringAsync();
				Console.WriteLine("Список текстов: " + json);
			}
			else
			{
				Console.WriteLine("Ошибка при получении текстов. Код статуса: " + response.StatusCode);
				Console.WriteLine("Текст ответа: " + await response.Content.ReadAsStringAsync());
			}
		}

		// (4) Зашифровать текст
		private static async Task EncryptTextAsync()
		{
			if (string.IsNullOrEmpty(_token))
			{
				Console.WriteLine("Сначала необходимо авторизоваться (token пуст).");
				return;
			}

			Console.Write("Введите text_id: ");
			string textIdStr = Console.ReadLine() ?? "0";
			if (!int.TryParse(textIdStr, out int textId))
			{
				Console.WriteLine("Ошибка: text_id должен быть числом!");
				return;
			}

			Console.Write("Введите ключ для шифрования (key): ");
			string key = Console.ReadLine() ?? "";

			_client.DefaultRequestHeaders.Authorization =
				new AuthenticationHeaderValue("Bearer", _token);

			// Пример: POST /encrypt?text_id=123&key=secret
			string url = $"/encrypt?text_id={textId}&key={Uri.EscapeDataString(key)}";

			var response = await _client.PostAsync(url, content: null);
			if (response.IsSuccessStatusCode)
			{
				string json = await response.Content.ReadAsStringAsync();
				var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);

				if (dict != null && dict.TryGetValue("encrypted_text", out string encryptedText))
				{
					Console.WriteLine("Зашифрованный текст: " + encryptedText);
				}
				else
				{
					Console.WriteLine("Не удалось получить поле 'encrypted_text' из ответа сервера.");
				}
			}
			else
			{
				Console.WriteLine("Ошибка при шифровании. Код статуса: " + response.StatusCode);
				Console.WriteLine("Текст ответа: " + await response.Content.ReadAsStringAsync());
			}
		}

		// (5) Дешифровать текст
		private static async Task DecryptTextAsync()
		{
			if (string.IsNullOrEmpty(_token))
			{
				Console.WriteLine("Сначала необходимо авторизоваться (token пуст).");
				return;
			}

			Console.Write("Введите text_id: ");
			string textIdStr = Console.ReadLine() ?? "0";
			if (!int.TryParse(textIdStr, out int textId))
			{
				Console.WriteLine("Ошибка: text_id должен быть числом!");
				return;
			}

			Console.Write("Введите ключ для дешифрования (key): ");
			string key = Console.ReadLine() ?? "";

			_client.DefaultRequestHeaders.Authorization =
				new AuthenticationHeaderValue("Bearer", _token);

			// Пример: POST /decrypt?text_id=123&key=secret
			string url = $"/decrypt?text_id={textId}&key={Uri.EscapeDataString(key)}";
			var response = await _client.PostAsync(url, content: null);
			if (response.IsSuccessStatusCode)
			{
				string json = await response.Content.ReadAsStringAsync();
				var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);

				if (dict != null && dict.TryGetValue("decrypted_text", out string decryptedText))
				{
					Console.WriteLine("Расшифрованный текст: " + decryptedText);
				}
				else
				{
					Console.WriteLine("Не удалось получить поле 'decrypted_text' из ответа сервера.");
				}
			}
			else
			{
				Console.WriteLine("Ошибка при дешифровании. Код статуса: " + response.StatusCode);
				Console.WriteLine("Текст ответа: " + await response.Content.ReadAsStringAsync());
			}
		}
		private static async Task AddTextAsync()
		{
			if (string.IsNullOrEmpty(_token))
			{
				Console.WriteLine("Сначала необходимо авторизоваться (token пуст).");
				return;
			}

			Console.Write("Введите содержимое текста (content): ");
			string content = Console.ReadLine() ?? "";

			// Устанавливаем заголовок "Authorization: Bearer <token>"
			_client.DefaultRequestHeaders.Authorization =
				new AuthenticationHeaderValue("Bearer", _token);

			// Формируем запрос: POST /text?content=...
			string url = $"/text?content={Uri.EscapeDataString(content)}";
			var response = await _client.PostAsync(url, content: null);

			if (response.IsSuccessStatusCode)
			{
				// Ожидаем, что сервер вернёт что-то вроде:
				// {
				//   "message": "Text added",
				//   "text_id": 123
				// }
				var json = await response.Content.ReadAsStringAsync();
				var dict = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

				if (dict != null)
				{
					Console.WriteLine("Текст успешно добавлен!");
					// Если нужно, покажем text_id
					if (dict.TryGetValue("text_id", out var textIdObj))
					{
						Console.WriteLine("Присвоенный ID текста: " + textIdObj);
					}
				}
				else
				{
					Console.WriteLine("Не удалось разобрать ответ сервера.");
				}
			}
			else
			{
				Console.WriteLine("Ошибка при добавлении текста. Код статуса: " + response.StatusCode);
				Console.WriteLine("Текст ответа: " + await response.Content.ReadAsStringAsync());
			}
		}

	}
}
