using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Website_ban_sach.Models;

namespace Website_ban_sach.Controllers
{
    public class PaymentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public PaymentController(
            ApplicationDbContext context,
            IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<IActionResult> CreateMomoPayment(int orderId)
        {
            
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
                return NotFound();

            string endpoint =
                "https://test-payment.momo.vn/v2/gateway/api/create";

            string partnerCode = _configuration["MoMo:PartnerCode"];
            string accessKey = _configuration["MoMo:AccessKey"];
            string secretKey = _configuration["MoMo:SecretKey"];

            string requestId = Guid.NewGuid().ToString();
            string momoOrderId = order.Id.ToString();

            long amount = (long)order.TotalAmount;

            string orderInfo =
                $"Thanh toan don hang #{order.Id}";

            string redirectUrl =
                _configuration["MoMo:ReturnUrl"];

            string ipnUrl =
                _configuration["MoMo:NotifyUrl"];

            string requestType = "captureWallet";

            string extraData = "";

            string rawHash =
                $"accessKey={accessKey}" +
                $"&amount={amount}" +
                $"&extraData={extraData}" +
                $"&ipnUrl={ipnUrl}" +
                $"&orderId={momoOrderId}" +
                $"&orderInfo={orderInfo}" +
                $"&partnerCode={partnerCode}" +
                $"&redirectUrl={redirectUrl}" +
                $"&requestId={requestId}" +
                $"&requestType={requestType}";

            string signature =
                CreateSignature(rawHash, secretKey);

            var requestData = new
            {
                partnerCode,
                partnerName = "BookStore",
                storeId = "BookStore",
                requestId,
                amount = amount.ToString(),
                orderId = momoOrderId,
                orderInfo,
                redirectUrl,
                ipnUrl,
                lang = "vi",
                requestType,
                autoCapture = true,
                extraData,
                signature
            };

            using HttpClient client = new();

            StringContent content =
                new StringContent(
                    JsonSerializer.Serialize(requestData),
                    Encoding.UTF8,
                    "application/json");

            HttpResponseMessage response =
                await client.PostAsync(endpoint, content);

            string responseBody =
                await response.Content.ReadAsStringAsync();

            using JsonDocument json =
                JsonDocument.Parse(responseBody);

            if (json.RootElement.TryGetProperty("payUrl", out var payUrl))
            {
                return Redirect(payUrl.GetString());
            }

            return Content(responseBody);
        }

        public async Task<IActionResult> Return()
        {
            string resultCode =
                Request.Query["resultCode"];

            string orderId =
                Request.Query["orderId"];

            if (!string.IsNullOrEmpty(orderId))
            {
                int id = int.Parse(orderId);

                var order =
                    await _context.Orders
                        .FirstOrDefaultAsync(x => x.Id == id);

                if (order != null)
                {
                    if (resultCode == "0")
                    {
                        order.Status = "Đã thanh toán";

                        var orderDetails =
                            await _context.OrderItems
                                .Include(x => x.Product)
                                .Where(x => x.OrderId == order.Id)
                                .ToListAsync();

                        foreach (var item in orderDetails)
                        {
                            item.Product.StockQuantity -= item.Quantity;
                        }
                    }
                    else
                    {
                        order.Status = "Thanh toán thất bại";
                    }

                    await _context.SaveChangesAsync();
                }
            }

            ViewBag.ResultCode = resultCode;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Notify()
        {
            using StreamReader reader =
                new StreamReader(Request.Body);

            string body = await reader.ReadToEndAsync();

            return Ok();
        }

        private static string CreateSignature(
            string rawData,
            string secretKey)
        {
            byte[] keyBytes =
                Encoding.UTF8.GetBytes(secretKey);

            byte[] messageBytes =
                Encoding.UTF8.GetBytes(rawData);

            using HMACSHA256 hmac =
                new HMACSHA256(keyBytes);

            byte[] hash =
                hmac.ComputeHash(messageBytes);

            return BitConverter
                .ToString(hash)
                .Replace("-", "")
                .ToLower();
        }
    }
}
