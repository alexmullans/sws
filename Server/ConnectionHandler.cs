﻿using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Timers;
using System.Net;
using Server.Protocol;

namespace Server
{
    class ConnectionHandler
    {
        private Server Server { get; set; }
        private Socket Socket { get; set; }
        public NetworkStream Stream { get; set; }

        public ConnectionHandler(Server server, Socket socket)
        {
            Server = server;
            Socket = socket;
        }

        internal void Handle()
        {
            var start = DateTime.Now;
            Stream = new NetworkStream(Socket);
            var streamReader = new StreamReader(Stream);

            HttpRequest request;
            HttpResponse response = null;

            try
            {
                request = new HttpRequest(streamReader);
            }
            catch (ProtocolException e)
            {
                //if (e.Status == Constants.BadRequestCode)
                response = HttpResponseFactory.CreateBadRequest(Constants.Close);
                response.Write(Stream);
                PrepareToReturn(start);
                return;
            }

            if (request.ProtocolVersion != HttpVersion.Version11)
            {
                response = HttpResponseFactory.CreateNotSupported(Constants.Close);
                response.Write(Stream);
                PrepareToReturn(start);
                return;
            }

            if (request.Method != "GET")
            {
                response = HttpResponseFactory.CreateNotImplemented(Constants.Close);
                response.Write(Stream);
                PrepareToReturn(start);
                return;
            }

            // request is GET from here on
            var path = Server.RootDirectory + request.Uri;
            if (Directory.Exists(path))
                path += "\\" + Constants.DefaultFile;
            response = File.Exists(path) ? HttpResponseFactory.CreateOk(path, Constants.Close)
                : HttpResponseFactory.CreateNotFound(Constants.Close);
            response.Write(Stream);
            // TODO: Support 304

            PrepareToReturn(start);
        }



        private void PrepareToReturn(DateTime start)
        {
            Stream.Close();
            Socket.Close(); // this should close the NetworkStream and StreamReader as well
            Server.IncrementStatistics(start);
        }


    }
}
