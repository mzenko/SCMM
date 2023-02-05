﻿using System.Net;
using System.Text.Json;

namespace SCMM.Market.Buff.Client
{
    public class BuffWebClient : Shared.Client.WebClient
    {
        private const string WebBaseUri = "https://buff.163.com/";
        private const string ApiBaseUri = "https://buff.163.com/api/";

        public const int MaxPageLimit = 80;

        public BuffWebClient(BuffConfiguration configuration, IWebProxy webProxy) : base(cookieContainer: new CookieContainer(), webProxy: webProxy)
        {
            Cookies.Add(new Uri(ApiBaseUri), new Cookie("session", configuration.SessionId));
        }

        public async Task<BuffMarketGoodsResponse> GetMarketGoodsAsync(string appName, int page = 1, int pageSize = MaxPageLimit)
        {
            using (var client = BuildWebBrowserHttpClient(referer: new Uri(WebBaseUri)))
            {
                var url = $"{ApiBaseUri}market/goods?game={appName.ToLower()}&page_num={page}&page_size={pageSize}&sort_by=price.desc&trigger=undefined_trigger&_={Random.Shared.NextInt64(1000000000000L, 9999999999999L)}";
                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                var textJson = await response.Content.ReadAsStringAsync();
                var responseJson = JsonSerializer.Deserialize<BuffResponse<BuffMarketGoodsResponse>>(textJson);
                return responseJson?.Data;
            }
        }
    }
}
