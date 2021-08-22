using Microsoft.Extensions.Options;
using MyCourse.Models.InputModels.Courses;
using MyCourse.Models.Options;
using PayPalCheckoutSdk.Core;
using PayPalCheckoutSdk.Orders;
using PayPalHttp;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace MyCourse.Models.Services.Infrastructure
{
    public class PaypalPaymentGateway : IPaymentGateway
    {
        private readonly IOptionsMonitor<PaypalOptions> options;

        public PaypalPaymentGateway(IOptionsMonitor<PaypalOptions> options)
        {
            this.options = options;
        }

        public async Task<string> GetPaymentUrlAsync(CoursePayInputModel inputModel)
        {
            OrderRequest order = new OrderRequest()
            {
                CheckoutPaymentIntent = "CAPTURE",
                ApplicationContext = new ApplicationContext()
                {
                    ReturnUrl = inputModel.ReturnUrl,
                    CancelUrl = inputModel.CancelUrl,
                    BrandName = options.CurrentValue.BrandName,
                    ShippingPreference = "NO_SHIPPING"
                },
                PurchaseUnits = new List<PurchaseUnitRequest>()
                {
                    new PurchaseUnitRequest()
                    {
                        CustomId = $"{inputModel.CourseId}/{inputModel.UserId}",
                        Description = inputModel.Description,
                        AmountWithBreakdown = new AmountWithBreakdown()
                        {
                            CurrencyCode = inputModel.Price.Currency.ToString(),
                            Value = inputModel.Price.Amount.ToString(CultureInfo.InvariantCulture) // 14.50
                        }
                    }
                }
            };

            PayPalEnvironment env = GetPayPalEnvironment(options.CurrentValue);
            PayPalHttpClient client = new PayPalHttpClient(env);

            OrdersCreateRequest request = new OrdersCreateRequest();
            request.RequestBody(order);
            request.Prefer("return=representation");

            HttpResponse response = await client.Execute(request); // contiene i dati grezzi
            Order result = response.Result<Order>(); // formattazione dei dati grezzi

            LinkDescription link = result.Links.Single(link => link.Rel == "approve");
            return link.Href;
        }

        public Task<CourseSubscribeInputModel> CapturePaymentAsync(string token)
        {
            throw new NotImplementedException();
        }

        private PayPalEnvironment GetPayPalEnvironment(PaypalOptions options)
        {
            string clientId = options.ClientId;
            string clientSecret = options.ClientSecret;

            if (options.IsSandbox)
            {
                return new SandboxEnvironment(clientId, clientSecret);
            }
            else
            {
                return new LiveEnvironment(clientId, clientSecret);
            }
        }
    }
}
