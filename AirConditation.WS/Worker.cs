using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AirConditation.WS
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Process'imize stop request'i gönderene kadar çalýþacak bir döngü
            while (!stoppingToken.IsCancellationRequested)
            {
                // rabbitMQ ya baðlantý. Localde çalýþtýðýmýz için username password girmemize gerek yok (default olarak guest'i kullanmaktadýr)

                var factory = new ConnectionFactory() { HostName = "localhost" };

                using (var connection = factory.CreateConnection())
                {
                    using (var channel = connection.CreateModel())
                    {
                        // bu kýsýmda okuyacaðýmýz queue yi belirtiyoruz
                        channel.QueueDeclare("air-conditation",
                           durable: false, // fiziksel mi saklanacak memory üzerinde mi?
                        exclusive: false, // farklý baðlantýlar ile kullaným
                        autoDelete: false, // consumer lar kullanýldýktan sonra otomatik silinmesi
                        arguments: null  // Exhange tipleri
                        );

                        // channel ý tüketecek consumer yaratýyoruz
                        var consumer = new EventingBasicConsumer(channel);

                        // oluþturduðumuz consumerýn hangi channel i tüketeceðini belirtiyoruz
                        channel.BasicConsume("air-conditation",
                            true,
                            consumer);

                        // kuyruktaki bekleyen mesajlarý okumaya baþlýyor
                        consumer.Received += (model, ea) =>
                        {
                            var body = ea.Body.ToArray();
                            var message = Encoding.UTF8.GetString(body);
                            if (message != null)
                            {
                                // kuyruktan aldýðýmýz mesajý örnek bir api ile iþlemeye gönderiyoruz.
                                string uri = $"http://api.openweathermap.org/data/2.5/weather?q={message}&lang=tr&appid=apikey";
                                WebRequest webRequest = WebRequest.Create(uri);
                                webRequest.Method = "POST";
                                webRequest.ContentType = "none";
                                webRequest.Timeout = 200000;//200 sn.
                                Stream dataStream = webRequest.GetRequestStream();
                                dataStream.Close();

                                WebResponse webResponse = webRequest.GetResponse();
                                var responseStream = webResponse.GetResponseStream();
                                StreamReader reader = new StreamReader(responseStream);
                                // okunan mesajýn api cevabýný logluyoruz
                                 Root response = JsonConvert.DeserializeObject<Root>(reader.ReadToEnd()); 

                                _logger.LogInformation(" [x] AirConditation {0} - {1}",message, response.weather.FirstOrDefault().description);

                                AirConditationRequestDTO dto = new AirConditationRequestDTO();
                                dto.country = message;
                                dto.description = response.weather.FirstOrDefault().description;

                                string postData = JsonConvert.SerializeObject(dto);

                                byte[] postByteArray = Encoding.UTF8.GetBytes(postData);

                                string uriUI = "https://localhost:44353/api/push";
                                webRequest = WebRequest.Create(uriUI);
                                webRequest.Method = "POST";
                                webRequest.ContentType = "application/json";
                                webRequest.Timeout = 200000;//200 sn.

                                Stream dataStreamUI = webRequest.GetRequestStream();
                                dataStreamUI.Write(postByteArray, 0, postByteArray.Length);
                                dataStreamUI.Close();

                                webResponse = webRequest.GetResponse();
                                responseStream = webResponse.GetResponseStream();


                            }
                        };
                        // her saniye dinlemeye yeniden baþlamasý için task delay atýyoruz.
                        await Task.Delay(10000, stoppingToken);


                    }

                }
            }
        }
    }
}
