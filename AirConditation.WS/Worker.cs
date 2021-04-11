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
            // Process'imize stop request'i g�nderene kadar �al��acak bir d�ng�
            while (!stoppingToken.IsCancellationRequested)
            {
                // rabbitMQ ya ba�lant�. Localde �al��t���m�z i�in username password girmemize gerek yok (default olarak guest'i kullanmaktad�r)

                var factory = new ConnectionFactory() { HostName = "localhost" };

                using (var connection = factory.CreateConnection())
                {
                    using (var channel = connection.CreateModel())
                    {
                        // bu k�s�mda okuyaca��m�z queue yi belirtiyoruz
                        channel.QueueDeclare("air-conditation",
                           durable: false, // fiziksel mi saklanacak memory �zerinde mi?
                        exclusive: false, // farkl� ba�lant�lar ile kullan�m
                        autoDelete: false, // consumer lar kullan�ld�ktan sonra otomatik silinmesi
                        arguments: null  // Exhange tipleri
                        );

                        // channel � t�ketecek consumer yarat�yoruz
                        var consumer = new EventingBasicConsumer(channel);

                        // olu�turdu�umuz consumer�n hangi channel i t�ketece�ini belirtiyoruz
                        channel.BasicConsume("air-conditation",
                            true,
                            consumer);

                        // kuyruktaki bekleyen mesajlar� okumaya ba�l�yor
                        consumer.Received += (model, ea) =>
                        {
                            var body = ea.Body.ToArray();
                            var message = Encoding.UTF8.GetString(body);
                            if (message != null)
                            {
                                // kuyruktan ald���m�z mesaj� �rnek bir api ile i�lemeye g�nderiyoruz.
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
                                // okunan mesaj�n api cevab�n� logluyoruz
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
                        // her saniye dinlemeye yeniden ba�lamas� i�in task delay at�yoruz.
                        await Task.Delay(10000, stoppingToken);


                    }

                }
            }
        }
    }
}
