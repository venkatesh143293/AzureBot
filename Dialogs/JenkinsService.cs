using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace CoreBot.Dialogs
{
    public class JenkinsService
    {
        protected readonly IConfiguration Configuration;
        private HttpClient getHttpClient()
        {
            var client = new HttpClient();
            client.BaseAddress = new Uri("http://usddccntr03.cotiviti.com:8585");
            client.DefaultRequestHeaders.Add("Authorization", "Basic " + Convert.ToBase64String(Encoding.Default.GetBytes("TestAuto:Ihealth@123")));
            return client;

        }
        public async Task<int> getLastBuild(string projectName)
        {
            try
            {
                var url = "http://usddccntr03.cotiviti.com:8585/job" + projectName +"/lastBuild/api/json";
                var client = getHttpClient();
                HttpResponseMessage response = client.GetAsync(url).Result;
                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadAsStringAsync();
                var res = JsonConvert.DeserializeObject<dynamic>(result);
                var lastbuildId = res["id"];
                return lastbuildId;
            }
            catch (Exception ex)
            {
                return 0;
            }
        }
 /*       public async Task<string> getLastBuildStatus(string projectName)
        {
            try
            {

             
                var url = "http://usddccntr01.cotiviti.com:8080/job/" + projectName+"/lastBuild/api/json";
                var client = getHttpClient();
                HttpResponseMessage response = client.GetAsync(url).Result;
                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadAsStringAsync();
                var res = JsonConvert.DeserializeObject<dynamic>(result);
                string lastbuildStatus = res["result"];
                if (string.IsNullOrEmpty(lastbuildStatus)|| lastbuildStatus=="{}")
                {
                    return "InProgress";
                }
                return lastbuildStatus;
            }
            catch (Exception ex)
            {
                return null;
            }
      }*/

        public async Task<dynamic> getLastBuildStatus(string projectName)
        {
            try
            {


                var url = "http://usddccntr03.cotiviti.com:8585/job/" + projectName + "/lastBuild/api/json";
                var client = getHttpClient();
                HttpResponseMessage response = client.GetAsync(url).Result;
                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadAsStringAsync();
                var res = JsonConvert.DeserializeObject<dynamic>(result);
                return res;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

    }
}
