using AirConditation.UI.Hubs;
using AirConditation.UI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AirConditation.UI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PushController : ControllerBase
    {
        private readonly IHubContext<ConditationHub> _hub;
        public PushController(IHubContext<ConditationHub> hub)
        {
            _hub = hub;
        }

        [HttpPost]
        public IActionResult Post([FromBody] AirConditationResponseDTO dto)
        {
            _hub.Clients.All.SendAsync("response", dto);

            return Accepted();

        }
    }
}
