namespace TowerDefencePackets
{
    public static class Header
    {
        //Could use an enum here, but I want implicit conversion to byte which I don't think I can do
        //So just do this instead

        /// <summary>
        /// Invalid packet
        /// </summary>
        public const char NUL                           = (char)0x0;

        /// <summary>
        /// Check if a username is available. Expects <see cref="string"/> parameter
        /// </summary>
        public const char REQUEST_USERNAME_AVAILABILITY = (char)0x01;

        /// <summary>
        /// Check if a username is available. Expects <see cref="string"/> parameter and expects <see cref="long"/> response
        /// </summary>
        public const char REQUEST_USERNAME              = (char)0x02;

        /// <summary>
        /// Inform the server that a client wants to connect to a lobby. Expects a <see cref="long"/> player id
        /// </summary>
        public const char REQUEST_LOBBY                 = (char)0x03;

        /// <summary>
        /// Tell the server to drop the connection for a user. Takes a <see cref="string"/> parameter
        /// </summary>
        public const char DISCONNECT                    = (char)0x04;

        /// <summary>
        /// Request how many active connections the server has. Returns <see cref="int"/>
        /// </summary>
        public const char REQUEST_TOTAL_CONNECTIONS     = (char)0x05;

        /// <summary>
        /// Tells a client that a lobby has been found. Returns <see cref="long"/> player id
        /// </summary>
        public const char CONNECT_LOBBY                 = (char)0x06;

        /// <summary>
        /// Pass a <see cref="long"/> to the server and returns the name associated with it
        /// </summary>
        public const char REQUEST_USERNAME_FROM_ID      = (char)0x07;

        /// <summary>
        /// Declares that a game is over. If the server receives this, it expects the id of the player who won. If the client receives this, it expects a byte. A non-zero bytes denotes that the client won
        /// </summary>
        public const char GAME_OVER                     = (char)0x08;
        
        /// <summary>
        /// Sends a serialised version of the loaded map to the client
        /// </summary>
        public const char RECEIVE_MAP_DATA              = (char)0x09;

        /// <summary>
        /// Sends a snapshot of an on-going game. Use <see cref="Snapshot"/> to serialise the data
        /// </summary>
        public const char SNAPSHOT                      = (char)0x0a;
    }
}