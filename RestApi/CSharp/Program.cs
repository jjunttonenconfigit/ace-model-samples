using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SampleRestApi
{
    class Program
  {
    private static readonly JsonSerializerOptions JsonSerializerOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    static async Task Main()
    {
      // TODO: Fill in the Ace Model address, login and password and Ace Platform ApiKey
      string address  = "";
      string login    = "";
      string password = "";
      string apiKey   = "";

      if (string.IsNullOrWhiteSpace(address)  ||
          string.IsNullOrWhiteSpace(login)    ||
          string.IsNullOrWhiteSpace(password) ||
          string.IsNullOrWhiteSpace(apiKey)) {
        throw new Exception("Sample cannot run without address and credentials");
      }

      // if address ends with slash remove it
      if (address.EndsWith("/") || address.EndsWith("\\")) {
        address = address.Substring(0, address.Length -1);
      }
      Console.WriteLine($"Using base address: '{address}'");


      NumericFamily family = null;

      family = await CreateSampleFamily(await GetClientUsingAceModelAuthorization(address, login, password));
      Console.WriteLine(family);

      family = await CreateSampleFamily(GetClientUsingAcePlatformAuthoriation(address, apiKey));
      Console.WriteLine(family);
    }

    private static async Task<HttpClient> GetClientUsingAceModelAuthorization(string baseAddress, string aceModelLogin, string aceModelPassword)
    {
      Console.WriteLine("Using Ace Model login and password authorization");

      // REQUIRED when using Ace Model authorization
      // You have to pass the session cookies with each request
      HttpClientHandler handler = new() { UseCookies = true };
      HttpClient client = new(handler);

      client.BaseAddress = new Uri(baseAddress);
      client.DefaultRequestHeaders.Accept.Clear();
      client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

      // initialize the session and get verification token
      var loginResponse = await client.PostAsync("/api/public/v1/auth/local/login", new JsonContent(new { Username = aceModelLogin, Password = aceModelPassword }));
      loginResponse.EnsureSuccessStatusCode();

      // extract and pass the token with each request
      var aceModelAuthorizationResponse = JsonSerializer.Deserialize<AceModelAuthorizationResponse>(await loginResponse.Content.ReadAsStringAsync(), JsonSerializerOptions);
      client.DefaultRequestHeaders.Add("X-RequestVerificationToken", aceModelAuthorizationResponse.Token);

      return client;
    }

    private static HttpClient GetClientUsingAcePlatformAuthoriation(string baseAddress, string apiKey)
    {
      Console.WriteLine("Using Ace Platform ApiKey authorization");

      HttpClient client = new();

      client.BaseAddress = new Uri(baseAddress);
      client.DefaultRequestHeaders.Accept.Clear();
      client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
      client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("ApiKey", apiKey);

      return client;
    }

    private static async Task<NumericFamily> CreateSampleFamily(HttpClient client)
    {
      string familyCode = "SAMPLE";
      Console.WriteLine($"Creating family with code {familyCode}");

      // 1. Create new Work Item
      var createWorkItemResponse = await client.PostAsync("/api/v1/wi/", new JsonContent(new { Name = "SampleWorkItem", Description = "Sample description" }));
      createWorkItemResponse.EnsureSuccessStatusCode();

      var workItem = JsonSerializer.Deserialize<WorkItem>(await createWorkItemResponse.Content.ReadAsStringAsync(), JsonSerializerOptions);
      var wi = workItem.Id;

      // 2. Using newly created Work Item add new SAMPLE family if does not exist
      NumericFamily family = null;
      var getFamilyResponse = await client.GetAsync($"/api/v1/wi/{wi}/library/families/{familyCode}");
      var getFamilyResponseContentJson = await getFamilyResponse.Content.ReadAsStringAsync();

      if (getFamilyResponse.StatusCode == HttpStatusCode.NotFound)
      {
        Console.WriteLine(getFamilyResponseContentJson);
      }
      else
      {
        getFamilyResponse.EnsureSuccessStatusCode();
        family = JsonSerializer.Deserialize<NumericFamily>(getFamilyResponseContentJson, JsonSerializerOptions);
      }

      // we already have SAMPLE family, so close the Work Item and return
      if (family != null)
      {
        Console.WriteLine($"Family {familyCode} already exists, closing Work Item {wi}");

        var closeWorkItemResponse = await client.PutAsync($"/api/v1/wi/{wi}/close", null);
        closeWorkItemResponse.EnsureSuccessStatusCode();

        return family;
      }

      // we do not have the family - create it
      NumericFamily newFamily = new()
      {
        Code        = familyCode,
        Description = "Test numeric family",
        LifeCycle   = "Concept",
        FamilyType  = "Numeric",
        Precision   = 2,
        MinValue    = 10m,
        MaxValue    = 100m
      };
      var createNewFamilyResponse = await client.PostAsync($"/api/v1/wi/{wi}/library/families", new JsonContent(newFamily));
      createNewFamilyResponse.EnsureSuccessStatusCode();
      family = JsonSerializer.Deserialize<NumericFamily>(await createNewFamilyResponse.Content.ReadAsStringAsync(), JsonSerializerOptions);

      Console.WriteLine($"Created new family {familyCode}, promote Work Item {wi}");

      // 3. Promote Work Item
      var promoteWorkItemResponse = await client.PutAsync($"/api/v1/wi/{wi}/promote", null);
      promoteWorkItemResponse.EnsureSuccessStatusCode();

      return family;
    }

    private class JsonContent : StringContent
    {
      public JsonContent(object obj) : base (JsonSerializer.Serialize(obj, JsonSerializerOptions), Encoding.UTF8, "application/json") {}
    }

    private class AceModelAuthorizationResponse
    {
      public string Token { get; set; }
    }

    private class WorkItem
    {
      public int    Id          { get; set; }
      public string Name        { get; set; }
      public string Description { get; set; }
    }

    private class NumericFamily
    {
      public string  Code        { get; set; }
      public string  Description { get; set; }
      public string  LifeCycle   { get; set; }
      public string  FamilyType  { get; set; }
      public int     Precision   { get; set; }
      public decimal MinValue    { get; set; }
      public decimal MaxValue    { get; set; }

      public override string ToString()
      {
        return
          $"Code:        {Code ?? "null"}\n"        +
          $"Description: {Description ?? "null"}\n" +
          $"LifeCycle:   {LifeCycle ?? "null"}\n"   +
          $"FamilyType:  {FamilyType ?? "null"}\n"  +
          $"Precision:   {Precision}\n"             +
          $"MinValue:    {MinValue}\n"              +
          $"MaxValue:    {MaxValue}\n";
      }
    }
  }
}

