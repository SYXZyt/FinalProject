namespace TowerDefencePackets
{
    public static class Header
    {
        //Could use an enum here, but I want implicit conversion to byte which I don't think I can do
        //So just do this instead
        public const char NUL                           = (char)0x0;
        public const char REQUEST_USERNAME_AVAILABILITY = (char)0x01;
        public const char REQUEST_USERNAME              = (char)0x02;
        public const char REQUEST_LOBBY                 = (char)0x03;
        public const char DISCONNECT                    = (char)0x04;
        public const char REQUEST_TOTAL_CONNECTIONS     = (char)0x05;
    }
}