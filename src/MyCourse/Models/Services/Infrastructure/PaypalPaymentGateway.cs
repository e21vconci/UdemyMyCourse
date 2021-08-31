using System;
using System.Linq;
using System.Globalization;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Options;
using PayPalCheckoutSdk.Core;
using PayPalCheckoutSdk.Orders;
using PayPalHttp;
using MyCourse.Models.Enums;
using MyCourse.Models.Options;
using MyCourse.Models.InputModels.Courses;
using MyCourse.Models.Exceptions.Infrastructure;

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

        public async Task<CourseSubscribeInputModel> CapturePaymentAsync(string token)
        {
            try
            {

                PayPalEnvironment env = GetPayPalEnvironment(options.CurrentValue); // sandbox per i test
                PayPalHttpClient client = new PayPalHttpClient(env);

                OrdersCaptureRequest request = new OrdersCaptureRequest(token);
                request.RequestBody(new OrderActionRequest());
                request.Prefer("return=representation");

                HttpResponse response = await client.Execute(request); // contiene i dati grezzi
                Order result = response.Result<Order>(); // formattazione dei dati grezzi (deserializzo l'ordine)

                PurchaseUnit purchaseUnit = result.PurchaseUnits.First();
                Capture capture = purchaseUnit.Payments.Captures.First();

                // $"{inputModel.CourseId}/{inputModel.UserId}" per ottenere il courseId e lo userId dal customId
                string[] customIdParts = purchaseUnit.CustomId.Split('/');
                int courseId = int.Parse(customIdParts[0]);
                string userId = customIdParts[1];

                return new CourseSubscribeInputModel
                {
                    CourseId = courseId,
                    UserId = userId,
                    Paid = new ValueTypes.Money(Enum.Parse<Currency>(capture.Amount.CurrencyCode), decimal.Parse(capture.Amount.Value, CultureInfo.InvariantCulture)),
                    TransactionId = capture.Id,
                    PaymentDate = DateTime.Parse(capture.CreateTime, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal),
                    PaymentType = "Paypal"
                };
            } 
            catch (Exception exc)
            {
                throw new PaymentGatewayException(exc);
            }
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
