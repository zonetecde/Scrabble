using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scrabble_Serveur.Serveur
{
    public class ClassLibrary
    {
        public class LastMessage
        {
            public string userID;
            public string lastMessage;

            public LastMessage(string userID, string lastMessage)
            {
                this.userID = userID;
                this.lastMessage = lastMessage;
            }
        }

        public class User
        {
            public string userID;
            public string userApp;
            public string lastMessage;

            public User(string userID)
            {
                this.userID = userID;
                userApp = string.Empty;
            }
        }

        public class Message
        {
            public Message(string id, string content, string appName, MESSAGE_TYPE messageType)
            {
                Id = id;
                Content = content;
                MessageType = messageType;
                AppName = appName;
            }

            public string Id { get; set; }
            public string Content { get; set; }
            public string AppName { get; set; }

            public List<LastMessage> LastMessages { get; set; }

            public MESSAGE_TYPE MessageType { get; set; }
        }

        public enum MESSAGE_TYPE
        {
            MESSAGE,
            CONNECTION,
            DISCONNECTION,
            LAST_MESSAGE,
            APP_NAME_INFORMATION,
        }
    }
}
