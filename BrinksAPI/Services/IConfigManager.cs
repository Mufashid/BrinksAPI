namespace BrinksAPI.Interfaces
{
    public interface IConfigManager
    {
        string URI { get; }
        string Username { get; }
        string Password { get; }
        string CompanyCode { get; }
        string ServiceDataProvider { get; }
        string EnterpriseId { get; }
        string ServerId { get; }

        string SftpUri { get; }
        string SftpUsername { get; }
        string SftpPassword { get; }
        string SftpOutboundFolder { get; }
        string SftpBackupFolder { get; }
    }


}
