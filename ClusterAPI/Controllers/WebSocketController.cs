using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.WebSockets;

namespace ClusterAPI.Controllers
{
    public class WebSocketController : ApiController
    {
        [HttpGet]
        public HttpResponseMessage GetMessage()

        {

            if (HttpContext.Current.IsWebSocketRequest)

            {

                HttpContext.Current.AcceptWebSocketRequest(ProcessRequestInternal);

            }

            return new HttpResponseMessage(HttpStatusCode.SwitchingProtocols);

        }

        private async Task ProcessRequestInternal(AspNetWebSocketContext ctx)

        {
            System.Diagnostics.Debug.WriteLine("In socket");

            WebSocket socket = ctx.WebSocket;

            await socket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes("Hello")), WebSocketMessageType.Text, true, CancellationToken.None);

            ArraySegment<byte> buffer = new ArraySegment<byte>(new byte[1024]);
            while (true)
            {
                var retVal = await socket.ReceiveAsync(buffer, CancellationToken.None);

                if (retVal.CloseStatus != null)
                {
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Bye", CancellationToken.None);
                    return;
                }

                await socket.SendAsync(new ArraySegment<byte>(buffer.Array, 0, retVal.Count), retVal.MessageType, retVal.EndOfMessage, CancellationToken.None);

            }

        }
    }
}
