using BrinksAPI.Interfaces;

namespace BrinksAPI.Services
{
    public class Config : IConfigManager
    {
        private readonly IConfiguration Configuration;

        public Config(IConfiguration _configuration)
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

        public string SftpUri
        {
            get
            {
                return Configuration["TransportBookingModule:Sftp:URI"];
            }
        }

        public string SftpUsername
        {
            get
            {
                return Configuration["TransportBookingModule:Sftp:Username"];
            }
        }

        public string SftpPassword
        {
            get
            {
                return Configuration["TransportBookingModule:Sftp:Password"];
            }
        }

        public string SftpOutboundFolder
        {
            get
            {
                return Configuration["TransportBookingModule:Sftp:OutboundFolder"];
            }
        }

        public string SftpBackupFolder
        {
            get
            {
                return Configuration["TransportBookingModule:Sftp:BackupFolder"];
            }
        }
    }
}

