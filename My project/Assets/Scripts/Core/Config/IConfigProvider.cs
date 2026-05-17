namespace TwelveMoons.Core.Config
{
    public interface IConfigProvider
    {
        bool CanLoad(string tableName);

        ConfigTable LoadTable(string tableName);
    }
}
