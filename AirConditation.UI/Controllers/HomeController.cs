using Microsoft.AspNetCore.Mvc;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AirConditation.UI.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }


        [HttpPost]
        public void Submit(string country)
        {

            // RabbitMQ ya bağlantı. Sunucu üzerinde ise IP bilgisi yer alacak
            var factory = new ConnectionFactory() { HostName = "localhost" };
            using (var connection = factory.CreateConnection())
            {
                // mesajı iletmek için kanal oluşturuyoruz
                using (var channel = connection.CreateModel())
                {
                    channel.QueueDeclare("air-conditation",
                        durable: false, // fiziksel mi saklanacak memory üzerinde mi?
                        exclusive: false, // farklı bağlantılar ile kullanım
                        autoDelete: false, // consumer lar kullanıldıktan sonra otomatik silinmesi
                        arguments: null); // Exhange tipleri

                   
                    var body = Encoding.UTF8.GetBytes(country);

                    channel.BasicPublish("",
                        "air-conditation",
                        null,
                        body);
                }
              
            }
        }
    }
}
