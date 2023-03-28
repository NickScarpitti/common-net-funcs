using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common_Net_Funcs.Web.JWT;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace Common_Net_Funcs.Web;

public static class DataPowerHelper
{

    public class DataPowerConnectionModel
    {
        public string postDestinationUrl { get; set; }
        public string guid { get; set; }
        public string authUrl { get; set; }
        public string authKey { get; set; }
        public string userId { get; set; }
        public string password { get; set; }
    }



    public static async Task<TokenObject> GetDataPowerApiToken(DataPowerConnectionModel dataPowerConnectionModel, string guid, string userId, DateTime dateTime)
    {

        Dictionary<string, string> httpPostHeaders = new()
        {
            { "hondaHeaderType.messageId", guid },
            { "hondaHeaderType.siteId", "honda.com" },
            { "hondaHeaderType.businessId", userId },
            { "hondaHeaderType.collectedTypestamp", dateTime.ToString("O") },
            { "Accept", "application/json" },
            { "X-Honda-wl-authorization", "Basic " + dataPowerConnectionModel.authKey },
            { "Authorization", "Basic " + dataPowerConnectionModel.authKey },
            { "Content-Type", "application/json" }
        };

        TokenObject DataPowerToken = await RestHelpers<TokenObject>.PostRequestWithCustomHeaders(dataPowerConnectionModel.authUrl, guid, httpPostHeaders) ?? new();

        return DataPowerToken;
    }
}
