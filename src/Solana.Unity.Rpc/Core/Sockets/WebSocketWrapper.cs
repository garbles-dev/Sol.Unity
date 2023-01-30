﻿using NativeWebSocket;
using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using WebSocket = NativeWebSocket.WebSocket;
using WebSocketState = System.Net.WebSockets.WebSocketState;

namespace Solana.Unity.Rpc.Core.Sockets
{
    internal class WebSocketWrapper : IWebSocket
    {
        private NativeWebSocket.IWebSocket webSocket;

        public WebSocketCloseStatus? CloseStatus => WebSocketCloseStatus.NormalClosure;

        public string CloseStatusDescription => "Not implemented";

        public WebSocketState State
        {
            get
            {
                if(webSocket == null)
                    return WebSocketState.None;
                return webSocket.State switch
                {
                    NativeWebSocket.WebSocketState.Open => WebSocketState.Open,
                    NativeWebSocket.WebSocketState.Closed => WebSocketState.Closed,
                    NativeWebSocket.WebSocketState.Connecting => WebSocketState.Connecting,
                    NativeWebSocket.WebSocketState.Closing => WebSocketState.CloseReceived,
                    _ => WebSocketState.None
                };
            }
        }

        public Task CloseAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken)
            => webSocket.Close();

        public Task ConnectAsync(Uri uri, CancellationToken cancellationToken)
        {
            webSocket = WebSocket.Create(uri.AbsoluteUri);
            return webSocket.Connect();
        }

        public Task CloseAsync(CancellationToken cancellationToken)
            => webSocket.Close();

        public Task<WebSocketReceiveResult> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            TaskCompletionSource<WebSocketReceiveResult> receiveMessageTask = new();

            void WebSocketOnOnMessage(byte[] bytes)
            {
                bytes.CopyTo(buffer);
                WebSocketReceiveResult webSocketReceiveResult = new(bytes.Length, WebSocketMessageType.Text, true);
                MainThreadUtil.Run(() => receiveMessageTask.SetResult(webSocketReceiveResult));
                webSocket.OnMessage -= WebSocketOnOnMessage;
                Console.WriteLine("Message received");
            }
            webSocket.OnMessage += WebSocketOnOnMessage;
            return receiveMessageTask.Task;
        }

        public Task SendAsync(ReadOnlyMemory<byte> buffer, WebSocketMessageType messageType, bool endOfMessage,
            CancellationToken cancellationToken)
        {
            return webSocket.Send(buffer.ToArray());
        }

        #region IDisposable Support
        private bool disposedValue; // To detect redundant calls

        private void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    webSocket.Close();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}