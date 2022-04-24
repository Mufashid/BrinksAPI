using Cargowise;

namespace BrinksAPI.Helpers
{

    public class Settings
    {
        public readonly IConfigManager Configuration;
        public Settings(IConfigManager _configuration)
        {
            Configuration = _configuration;
        }
        
    }
    
}
