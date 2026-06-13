using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using meow.Models;

namespace meow.Controllers
{
    public class AIChatController : Controller
    {
        private readonly LibraryDbContext _context;

        public AIChatController(LibraryDbContext context) => _context = context;

        [HttpPost]
        public async Task<IActionResult> Ask(string prompt)
        {
            try
            {
                var books = await _context.Books.Take(50).ToListAsync();
                var booksInfo = string.Join("\n", books.Select(b => 
                    $"- ID: {b.Id}, Tytuł: {b.Tytul}, Autor: {b.Autor}"));

                using var client = new HttpClient();
                
                var requestBody = new 
                { 
                    model = "mistral", 
                    prompt = prompt, 
                    stream = false,
                    system = $@"Jesteś asystentem. Szukaj książki w bazie. 
                    Odpowiadaj WYŁĄCZNIE w formacie JSON bez żadnego dodatkowego tekstu: 
                    {{ ""found"": true, ""bookId"": ID_KSIĄŻKI, ""message"": ""Krótka odpowiedź"" }}
                    Baza: {booksInfo}"
                };
                
                var response = await client.PostAsJsonAsync("http://host.docker.internal:11434/api/generate", requestBody);
                
                if (!response.IsSuccessStatusCode) 
                    return Json(new { success = false, message = "Ollama nie odpowiada" });

                var content = await response.Content.ReadFromJsonAsync<JsonElement>();
                var aiRawText = content.GetProperty("response").GetString();

                try 
                {
                    var aiJson = JsonSerializer.Deserialize<JsonElement>(aiRawText);
                    bool isFound = aiJson.GetProperty("found").GetBoolean();
                    int bookId = aiJson.GetProperty("bookId").GetInt32();

                    if (isFound && bookId > 0)
                    {
                        return Json(new { 
                            success = true, 
                            redirect = true, 
                            url = "/Shop/Details/" + bookId 
                        });
                    }
                    
                    return Json(new { success = true, redirect = false, message = aiJson.GetProperty("message").GetString() });
                }
                catch
                {
                    return Json(new { success = true, redirect = false, message = aiRawText });
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}