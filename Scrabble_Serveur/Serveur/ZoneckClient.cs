using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using static Scrabble_Serveur.Serveur.ClassLibrary;

namespace Scrabble_Serveur.Serveur
{
    public class ZoneckClient
    {
        private Socket SocketClient { get; set; }
        public string ConnetionId { get; set; }
        public string AppName { get; set; }
        internal Action<Message> Receive { get; set; }

        internal bool _ZoneckClient(string appName, string ip, int port, Action<Message> Receive)
        {
            try
            {
                // Créer une instance du SocketClient
                SocketClient = new Socket(SocketType.Stream, ProtocolType.Tcp);
                IPAddress _ip = IPAddress.Parse(ip);
                IPEndPoint point = new IPEndPoint(_ip, port);

                // Connexion
                SocketClient.Connect(point);

                // Thread pour recevoir les messages du serveur en continu
                Thread thread = new Thread(_Receive);
                thread.IsBackground = true;
                thread.Start();

                // Le nom de l'application qui précède le message de connexion
                AppName = appName;

                this.Receive = Receive;

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Envois un message au serveur
        /// </summary>
        /// <param name="str">Le message à envoyer</param>
        internal void Send(string str)
        {
            // id > message 
            string msg;

            if(str.Contains("[last-message]"))
                msg = JsonConvert.SerializeObject(new Message(ConnetionId, str, AppName, MESSAGE_TYPE.LAST_MESSAGE)) + "\r\n";
            else
                msg = JsonConvert.SerializeObject(new Message(ConnetionId, str, AppName, MESSAGE_TYPE.MESSAGE)) + "\r\n";

            var buffter = Encoding.UTF8.GetBytes(msg);
            var temp = SocketClient.Send(buffter);
        }

        /// <summary>
        /// Reçois un message du serveur
        /// </summary>
        private void _Receive()
        {
            while (true)
            {
                // Recupère le message reçu
                byte[] buffer = new byte[1024 * 1024 * 2];
                var effective = SocketClient.Receive(buffer);

                var message = Encoding.UTF8.GetString(buffer, 0, effective);

                if (!string.IsNullOrEmpty(message))
                {
                    Message received_message = JsonConvert.DeserializeObject<Message>(message);

                    // Si c'est pour informer de son Id de session et informer ensuite le serveur que nous sommes de l'App (nouvelle connexion)
                    if (received_message.MessageType == MESSAGE_TYPE.CONNECTION)
                    {
                        ConnetionId = received_message.Id;

                        // Renvois le nom de l'appName en échange
                        Message msg = new Message(ConnetionId, string.Empty, AppName, MESSAGE_TYPE.APP_NAME_INFORMATION);
                        var buffter = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(msg) + "\r\n");
                        var temp = SocketClient.Send(buffter);
                    }
                    // Si le message est une réponse à une demande de %last_message% 
                    else if (received_message.MessageType == MESSAGE_TYPE.LAST_MESSAGE) // si c'est un last message
                    {
                        List<LastMessage> lastMessages = JsonConvert.DeserializeObject<List<LastMessage>>(received_message.Content);

                        Receive(
                        new Message(string.Empty, string.Empty, string.Empty, MESSAGE_TYPE.LAST_MESSAGE) { LastMessages = lastMessages }
                        );
                    }
                    else
                        Receive(received_message);
                }
            }
        }
    }
}