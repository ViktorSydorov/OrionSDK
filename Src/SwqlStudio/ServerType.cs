namespace SwqlStudio
{
    public class ServerType
    {
        public string Type { get; set; }
        public bool IsAuthenticationRequired { get; set; }
        public bool IsTokenBasedAuthentication { get; internal set; } = false;
    }
}
