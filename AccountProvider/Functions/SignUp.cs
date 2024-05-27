using AccountProvider.Models;
using Data.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;

namespace AccountProvider.Functions
{
    public class SignUp
    {
        private readonly ILogger<SignUp> _logger;
        private readonly UserManager<UserAccount> _userManager;
        private readonly ServiceBusClient _serviceBusClient;
        private const string QueueName = "verification_request";

        public SignUp(ILogger<SignUp> logger, UserManager<UserAccount> userManager, ServiceBusClient serviceBusClient)
        {
            _logger = logger;
            _userManager = userManager;
            _serviceBusClient = serviceBusClient;
        }

        [Function("SignUp")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            string body = null!;
            try
            {
                body = await new StreamReader(req.Body).ReadToEndAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"StreamReader :: {ex.Message}");
            }

            if (body != null)
            {
                UserRegistrationRequest urr = null!;
                try
                {
                    urr = JsonConvert.DeserializeObject<UserRegistrationRequest>(body)!;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"JsonConvert.DeserializeObject<UserRegistrationRequest> :: {ex.Message}");
                }

                if (urr != null && urr.Email != null && urr.Password != null)
                {
                    if (!await _userManager.Users.AnyAsync(x => x.Email == urr.Email))
                    {
                        var userAccount = new UserAccount
                        {
                            FirstName = urr.FirstName,
                            LastName = urr.LastName,
                            Email = urr.Email,
                            UserName = urr.Email
                        };

                        try
                        {
                            var result = await _userManager.CreateAsync(userAccount, urr.Password);
                            if (result.Succeeded)
                            {
                                // Send message to Service Bus
                                await SendMessageToServiceBus(userAccount.Email);
                                return new OkResult();
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"_userManager.CreateAsync :: {ex.Message}");
                        }
                    }
                    else
                    {
                        return new ConflictResult();
                    }
                }
            }
            return new BadRequestResult();
        }

        private async Task SendMessageToServiceBus(string email)
        {
            try
            {
                ServiceBusSender sender = _serviceBusClient.CreateSender(QueueName);

                string jsonMessage = JsonConvert.SerializeObject(new { Email = email });

                ServiceBusMessage message = new ServiceBusMessage(jsonMessage);

                await sender.SendMessageAsync(message);

                _logger.LogInformation($"Sent message to Service Bus: {email}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"SendMessageToServiceBus :: {ex.Message}");
            }
        }

    }
}
