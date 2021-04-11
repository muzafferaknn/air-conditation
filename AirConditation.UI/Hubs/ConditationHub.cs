using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AirConditation.UI.Hubs
{
    public class ConditationHub :Hub
    {
        // bağlantı açmak için override ediyoruz
        public override Task OnConnectedAsync()
        {
            return base.OnConnectedAsync();
        }

        // bağlantı kapatmak için override ediyoruz
        public override Task OnDisconnectedAsync(Exception exception)
        {
            return base.OnDisconnectedAsync(exception);
        }
    }
}
