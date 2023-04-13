using Common_Net_Funcs.Web.JWT;

namespace Common_Net_Funcs.Web;

public static class DataPowerHelper
{

    public class DataPowerConnectionModel
    {
        public string? postDestinationUrl { get; set; }
        public string? guid { get; set; }
        public string? authUrl { get; set; }
        public string? authKey { get; set; }
        public string? userId { get; set; }
        public string? password { get; set; }
    }



    public static async Task<TokenObject> GetDataPowerApiToken(DataPowerConnectionModel dataPowerConnectionModel, string guid, string userEmail, DateTime dateTime)
    {

        Dictionary<string, string> httpPostHeaders = new()
        {
            { "hondaHeaderType.messageId", guid },
            { "hondaHeaderType.siteId", "honda.com" },
            { "hondaHeaderType.businessId", userEmail },
            { "hondaHeaderType.collectedTypestamp", dateTime.ToString("O") },
            { "Accept", "application/json" },
            { "X-Honda-wl-authorization", "Basic " + dataPowerConnectionModel?.authKey },
            { "Authorization", "Basic " + dataPowerConnectionModel?.authKey }//,
            //{ "Content-Type", "application/json" }
        };

        Dictionary<string, string> JsonPostObject = new() { { "ApiGuid", $"{dataPowerConnectionModel?.guid}" } };

        TokenObject DataPowerToken = await RestHelpers<TokenObject>.GenericPostRequest(dataPowerConnectionModel?.authUrl ?? string.Empty, JsonPostObject, httpHeaders: httpPostHeaders) ?? new();

        return DataPowerToken;
    }
}
