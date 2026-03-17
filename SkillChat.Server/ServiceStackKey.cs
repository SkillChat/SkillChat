using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using ServiceStack;

namespace SkillChat.Server
{
    public class ServiceStackKey
    {
        private static readonly HttpClient _httpClient = new HttpClient();

        IConfiguration configuration;
        ServiceStackSettings settings;

        public void Register(IConfiguration Configuration)
        {
            //configuration = Locator.Current.GetService<IConfiguration>(); после этого не находит settings, "Object reference not set to an instance of an object."
            configuration = Configuration;
            settings = configuration.GetSection("ServiceStackSettings").Get<ServiceStackSettings>();

            try
            {
                var serviceStackKey = settings.LicenseKey;
                Licensing.RegisterLicense(serviceStackKey);
            }

            catch (LicenseException)
            {
                var licenseKeyAddress = settings.LicenseKeyAddress;
                var newTrialKey = GetNewTrialKeyFromHtmlTextAsync(licenseKeyAddress).GetAwaiter().GetResult();

                settings.LicenseKey = newTrialKey;

                configuration.GetSection("ServiceStackSettings").Set(settings);

                var newServiceStackKey = settings.LicenseKey;

                Licensing.RegisterLicense(newServiceStackKey);
            }
        }

        private async Task<string> GetNewTrialKeyFromHtmlTextAsync(string url)
        {
            string htmlText = await _httpClient.GetStringAsync(url);
            int newKeyFirstIndex = htmlText.IndexOf("TRIAL");
            string newTrialKey = htmlText.Substring(newKeyFirstIndex, 383);
            return newTrialKey;
        }

    }
}
