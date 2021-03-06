﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NemoServer
{
    class Program
    {
        private static int _currentOrderId { get; set; }

        static NetworkStream stream;
        //  task = new Task(() => Listen());
        static Task task = new Task(() => FromClient());
        static TcpClient client;

        private static List<Order> _orders { get; set; } = new List<Order>();

        public static void Main()
        {
            Task task = new Task(ChefListener);
            task.Start();

            TcpListener server = new TcpListener(IPAddress.Parse("127.0.0.1"), 8080);
            server.Start();
            Console.WriteLine("Server has started on 127.0.0.1:8080.{0}Waiting for a connection...", Environment.NewLine);
            client = server.AcceptTcpClient();
            Console.WriteLine("A client connected.");
            stream = client.GetStream();
            //enter to an infinite cycle to be able to handle every change in stream

            while (true)
            {
                while (!stream.DataAvailable) ;
                Byte[] bytes = new Byte[client.Available];

                stream.Read(bytes, 0, bytes.Length);

                //translate bytes of request to string
                String data = Encoding.UTF8.GetString(bytes);

                //if (Regex.IsMatch(data, "^GET"))
                //{

                //}
                if (new Regex("^GET").IsMatch(data))
                {
                    Console.WriteLine("OK");
                    Byte[] response = Encoding.UTF8.GetBytes("HTTP/1.1 101 Switching Protocols" + Environment.NewLine
                        + "Connection: Upgrade" + Environment.NewLine
                        + "Upgrade: websocket" + Environment.NewLine
                        + "Sec-WebSocket-Accept: " + Convert.ToBase64String(
                            SHA1.Create().ComputeHash(
                                Encoding.UTF8.GetBytes(
                                    new Regex("Sec-WebSocket-Key: (.*)").Match(data).Groups[1].Value.Trim() + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11"
                                )
                            )
                        ) + Environment.NewLine
                        + Environment.NewLine);

                    stream.Write(response, 0, response.Length); //Avsluta handskakningen
                                                                // task.Start();
                    FromClient();
                }
                else
                {

                }

            }
        }

        private static void ChefListener()
        {
            while(true)
            {

                var orders = _orders.Where(x => x.ReadyStatus == false);
                if (orders.Count() > 0)
                {
                    var order = orders.First();

                    ToClient("Preparing order id: " + _currentOrderId + ": " + order.DishName);

                    Console.WriteLine("Order #:" + order.OrderNumber + " " + order.DishName + " ready?");
                    Console.ReadLine();

                    order.ReadyStatus = true;

                    ToClient("Order id: " + _currentOrderId + ": " + order.DishName + " ready.");
                }
            }
        }

        static void FromClient()
        {

            while (true)
            {
                // Console.WriteLine("FromClien");
                var bytes = new Byte[1024];
                int rec = stream.Read(bytes, 0, 1024);  //Blocking

                
                var length = bytes[1] - 128; //message length
                Byte[] key = new Byte[4];
                Array.Copy(bytes, 2, key, 0, key.Length);
                byte[] encoded = new Byte[length];
                byte[] decoded = new Byte[length];
                Array.Copy(bytes, 6, encoded, 0, encoded.Length);
                for (int i = 0; i < encoded.Length; i++)
                {
                    decoded[i] = (Byte)(encoded[i] ^ key[i % 4]);
                }
                var data = Encoding.UTF8.GetString(decoded);

                _currentOrderId++;
                _orders.Add(new Order { OrderNumber = _currentOrderId, DishName = data });

                //Console.WriteLine("Order id: " + _currentOrderId + ": " + data);
                if (data == "exit") break;
                ToClient(data);
            }
            stream.Close();
            client.Close();
        }
        static void ToClient(string input)
        {
            var s = input;
            var message = Encoding.UTF8.GetBytes(s);
            var send = new byte[message.Length + 2];
            send[0] = 0x81;
            send[1] = (byte)(message.Length); //Datal�ngd dvs antal bytes
            for (var i = 0; i < message.Length; i++)
            {
                send[i + 2] = (byte)message[i];
            }
            //byte[] send = new byte[3 + 2];
            //send[0] = 0x81; // last frame, text
            //send[1] = 3; // not masked, length 3
            //send[2] = 0x41;
            //send[3] = 0x42;
            //send[4] = 0x43;
            stream.Write(send, 0, send.Length);


        }

    }

}