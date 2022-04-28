using Microsoft.Extensions.Configuration;

namespace Cargowise
{
    public class CargoWiseConfig:IConfigManager
    {
        private readonly IConfiguration Configuration;

        public CargoWiseConfig(IConfiguration _configuration)
        {
            Configuration = _configuration;
        }
        public string URI
        {
            get
            {
                return Configuration["eAdaptor:URI"];
            }
        }
        public string Username
        {
            get
            {
                return Configuration["eAdaptor:Username"];
            }
        }
        public string Password
        {
            get
            {
                return Configuration["eAdaptor:Password"];
            }
        }
        public string ServerId
        {
            get
            {
                return Configuration["DataContext:ServerId"];
            }
        }
        public string EnterpriseId
        {
            get
            {
                return Configuration["DataContext:EnterpriseId"];
            }
        }
        public string ServiceDataProvider
        {
            get
            {
                return Configuration["DataContext:ServiceDataProvider"];
            }
        }
        public string CompanyCode
        {
            get
            {
                return Configuration["DataContext:CompanyCode"];
            }
        }     
    }
}
