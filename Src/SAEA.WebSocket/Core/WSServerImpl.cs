﻿/****************************************************************************
*项目名称：SAEA.WebSocket.Core
*CLR 版本：4.0.30319.42000
*机器名称：WENLI-PC
*命名空间：SAEA.WebSocket.Core
*类 名 称：WSServerImpl
*版 本 号：V1.0.0.0
*创建人： yswenli
*电子邮箱：wenguoli_520@qq.com
*创建时间：2019/6/13 15:37:28
*描述：
*=====================================================================
*修改时间：2019/6/13 15:37:28
*修 改 人： yswenli
*版 本 号： V1.0.0.0
*描    述：
*****************************************************************************/
using SAEA.Sockets;
using SAEA.WebSocket.Model;
using SAEA.WebSocket.Type;
using System;

namespace SAEA.WebSocket.Core
{
    /// <summary>
    /// websocket server,
    /// iocp实现
    /// </summary>
    internal class WSServerImpl : IWSServer
    {

        IServerSokcet _server;

        int _heartSpan = 20 * 1000;

        public event Action<string, WSProtocal> OnMessage;

        public WSServerImpl(int port = 39654, int bufferSize = 1024, int count = 60000)
        {
            var option = SocketOptionBuilder.Instance
                .SetSocket()
                .UseIocp(new WSContext())
                .SetPort(port)
                .SetReadBufferSize(bufferSize)
                .SetWriteBufferSize(bufferSize)
                .SetCount(count)
                .Build();

            _server = SocketFactory.CreateServerSocket(option);

            _server.OnReceive += _server_OnReceive;
        }

        private void _server_OnReceive(object currentObj, byte[] data)
        {
            var ut = (WSUserToken)(currentObj);

            if (!ut.IsHandSharked)
            {
                byte[] resData = null;

                var result = ut.GetReplayHandShake(data, out resData);

                if (result)
                {
                    _server.SendAsync(ut.ID, resData);
                    ut.IsHandSharked = true;
                }
            }
            else
            {
                var coder = (WSCoder)ut.Unpacker;
                coder.Unpack(data, (d) =>
                {
                    var wsProtocal = (WSProtocal)d;
                    switch (wsProtocal.Type)
                    {
                        case (byte)WSProtocalType.Close:
                            ReplyClose(ut.ID, wsProtocal);
                            break;
                        case (byte)WSProtocalType.Ping:
                            ReplyPong(ut.ID, wsProtocal);
                            break;
                        case (byte)WSProtocalType.Binary:
                        case (byte)WSProtocalType.Text:
                        case (byte)WSProtocalType.Cont:
                            OnMessage?.Invoke(ut.ID, (WSProtocal)d);
                            break;
                        case (byte)WSProtocalType.Pong:
                            break;
                        default:
                            var error = string.Format("收到未定义的Opcode={0}", d.Type);
                            break;
                    }

                }, (h) => { }, null);
            }
        }


        private void ReplyBase(string id, WSProtocalType type, byte[] content)
        {
            var byts = new WSProtocal(type, content).ToBytes();

            _server.SendAsync(id, byts);
        }

        private void ReplyBase(string id, WSProtocal data)
        {
            var byts = data.ToBytes();

            _server.SendAsync(id, byts);
        }

        private void ReplyPong(string id, WSProtocal data)
        {
            ReplyBase(id, WSProtocalType.Pong, data.Content);
        }

        private void ReplyClose(string id, WSProtocal data)
        {
            ReplyBase(id, WSProtocalType.Close, data.Content);
        }


        /// <summary>
        /// 回复客户端消息
        /// </summary>
        /// <param name="id"></param>
        /// <param name="data"></param>
        public void Reply(string id, WSProtocal data)
        {
            ReplyBase(id, data);
        }

        /// <summary>
        /// 发送关闭
        /// </summary>
        /// <param name="id"></param>
        /// <param name="data"></param>
        public void Disconnect(string id, WSProtocal data)
        {
            ReplyBase(id, data);

        }


        public void Start(int backlog=10000)
        {
            _server.Start(backlog);
        }

        public void Stop()
        {
            _server.Stop();
        }
    }

}
