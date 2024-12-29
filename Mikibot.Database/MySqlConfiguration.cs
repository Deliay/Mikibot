namespace Mikibot.Database;

public struct MySqlConfiguration
{
    public string Host { get; set; }
    public uint Port { get; set; }
    public string User { get; set; }
    public string Password { get; set; }
    public string Database { get; set; }

    public static MySqlConfiguration FromEnviroment()
    {
        return new MySqlConfiguration()
        {
            Host = Environment.GetEnvironmentVariable("MYSQL_HOST")!,
            Port = uint.Parse(Environment.GetEnvironmentVariable("MYSQL_PORT")!),
            User = Environment.GetEnvironmentVariable("MYSQL_USER")!,
            Password = Environment.GetEnvironmentVariable("MYSQL_PASSWORD")!,
            Database = Environment.GetEnvironmentVariable("MYSQL_DATABASE")!,
        };
    }
}